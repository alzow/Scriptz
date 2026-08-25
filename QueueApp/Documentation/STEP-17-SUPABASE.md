# Step 17 — required Supabase changes

## Read this first

Step 17b's brief says 17a is a prerequisite already applied. I can't confirm that from here: the
live-schema doc in this repo (`SUPABASE-SCHEMA-VERIFIED.md`, generated from an actual
`information_schema`/`pg_catalog` query against your project) has no trace of it — no
`allow_operator_choice` or `progress_status` columns, no `get_available_slots_any` /
`create_booking_any` / `set_queue_progress` / `set_booking_progress` functions, and its function
list is current only through "Step 16b." Either 17a was applied outside what that doc captured
(plausible — it isn't auto-regenerated), or it's still pending. Either way, I don't have 17a's
actual SQL to go on, only what 17b's prose implies about it, so **this documents both 17a and 17b
together** as one proposed migration, rather than assuming 17a's shape. Review it against
whatever you actually ran (if anything) before applying.

The C# side (models, services, view models, XAML) in this PR is fully wired up to call everything
below. `USE_STUBS` builds work today without any of it — the stubs simulate pooled join/booking
and progress notes in-memory — so you can review the UI immediately.

## The key design point: no new tables

The brief describes "resources" (bays, etc.) as if they might be a new concept, but they don't
need to be — `operators` already **is** the resource pool. `OperatorQueuePageViewModel` already
renders one column per operator row, plus an "Any available" column for entries with a null
`operator_id` (built well before this step). A car wash just needs four `operators` rows named
"Bay 1"–"Bay 4" instead of stylist names — nothing about the `operators` table's shape needs to
change. The only new schema is two columns and some function logic:

```sql
alter table businesses add column if not exists allow_operator_choice bool not null default true;
alter table queue_entries add column if not exists progress_status text;
alter table bookings add column if not exists progress_status text;
```

`default true` means every existing business is unaffected until you explicitly flip one to
`false`.

## 1. `start_serving` — assign a resource when the entry doesn't have one

I don't have your actual `start_serving` source, so I can't give you a drop-in replacement — only
the change it needs: when the target entry's `operator_id is null` (a pooled join), pick a free
operator at that business instead of requiring one to already be set. "Free" = no other entry
currently `'serving'` for that operator. Sketch:

```sql
-- Inside start_serving(p_entry_id uuid), before the existing serving_at/status update:
if (select operator_id from queue_entries where id = p_entry_id) is null then
  update queue_entries
  set operator_id = (
    select o.id
    from operators o
    where o.business_id = (select business_id from queue_entries where id = p_entry_id)
      and o.is_active
      and not exists (
        select 1 from queue_entries qe2
        where qe2.operator_id = o.id and qe2.status = 'serving'
      )
    order by o.sort_order
    limit 1
  )
  where id = p_entry_id;

  if (select operator_id from queue_entries where id = p_entry_id) is null then
    raise exception 'all resources are currently busy';
  end if;
end if;
-- ... existing status = 'serving', serving_at = now() logic continues unchanged
```

The exception message is surfaced verbatim in the app now (`OperatorQueuePageViewModel` overrides
`HandleExceptionAsync` to show it via a popup instead of silently logging it — see §5).

## 2. `set_queue_progress` / `set_booking_progress`

Straightforward owner-only column updates, returning the full row (matching every other mutation
function's shape):

```sql
create or replace function set_queue_progress(p_entry_id uuid, p_status text)
returns queue_entries
language plpgsql
security definer
set search_path = public
as $$
declare
  result queue_entries;
begin
  update queue_entries
  set progress_status = nullif(trim(p_status), '')
  where id = p_entry_id
    and is_business_owner(business_id)
  returning * into result;

  if result.id is null then
    raise exception 'not found or not permitted';
  end if;

  return result;
end;
$$;

grant execute on function set_queue_progress(uuid, text) to authenticated;

create or replace function set_booking_progress(p_booking_id uuid, p_status text)
returns bookings
language plpgsql
security definer
set search_path = public
as $$
declare
  result bookings;
begin
  update bookings
  set progress_status = nullif(trim(p_status), '')
  where id = p_booking_id
    and is_business_owner(business_id)
  returning * into result;

  if result.id is null then
    raise exception 'not found or not permitted';
  end if;

  return result;
end;
$$;

grant execute on function set_booking_progress(uuid, text) to authenticated;
```

`nullif(trim(p_status), '')` turns a cleared/whitespace-only input back into `null` rather than
storing an empty string, so `HasProgress` (`!string.IsNullOrWhiteSpace`) on the C# side correctly
treats a cleared note as "no progress" again.

## 3. `get_available_slots_any` / `create_booking_any`

I don't have your `get_available_slots`/`create_booking` source either, so — same as §1 — this is
a proposed shape to adapt to whatever those actually do (est_minutes-based grid, lead time, etc.),
not a verbatim replacement.

```sql
create or replace function get_available_slots_any(p_business_id uuid, p_service_id uuid, p_date date)
returns table (slot_start timestamptz, slot_end timestamptz, free_count int)
language sql
security definer
set search_path = public
as $$
  select slot_start, slot_end, count(*)::int as free_count
  from operators o
  cross join lateral get_available_slots(o.id, p_service_id, p_date) s
  where o.business_id = p_business_id and o.is_active
  group by slot_start, slot_end
  order by slot_start;
$$;

grant execute on function get_available_slots_any(uuid, uuid, date) to authenticated;
```

```sql
create or replace function create_booking_any(
  p_business_id uuid, p_service_id uuid, p_customer_id uuid,
  p_starts_at timestamptz, p_note text default null
)
returns bookings
language plpgsql
security definer
set search_path = public
as $$
declare
  op record;
  result bookings;
begin
  -- Try each active operator in order; the bookings_no_overlap exclusion constraint (§2 of the
  -- schema doc) is what actually prevents a double-booking — this just walks the resource list
  -- until one insert succeeds, rather than pre-computing who's free.
  for op in
    select id from operators
    where business_id = p_business_id and is_active
    order by sort_order
  loop
    begin
      result := create_booking(p_business_id, op.id, p_service_id, p_customer_id, p_starts_at, p_note);
      return result;
    exception when exclusion_violation then
      continue; -- that resource is booked at this time — try the next one
    end;
  end loop;

  raise exception 'that time was just taken';
end;
$$;

grant execute on function create_booking_any(uuid, uuid, uuid, timestamptz, text) to authenticated;
```

This assumes `create_booking` is callable as a plain SQL function (not only reachable via RPC) and
that its signature is `(business_id, operator_id, service_id, customer_id, starts_at, note)` —
adjust to match. If it isn't structured for internal reuse, the loop-and-catch logic can be
inlined directly against `bookings` instead.

`SlotResponse.FreeCount` (C# side) is populated from this union query's `free_count` column and
stays `null` on the single-operator path, which never selects it.

## 4. Customer-facing progress on the cross-business dashboard hero

Not part of 17a/17b's own scope, but `progress_status` needs to reach `my_active_queue_entry()`
too — that RPC (from the earlier dashboard-redesign PR) is what feeds the Browse tab's "you're in
the queue" hero card, which is the primary place a customer actually sees their status now. Add
`progress_status` to its output alongside the existing columns:

```sql
-- Add to my_active_queue_entry()'s returns table(...): progress_status text
-- Add to its final select list: m.progress_status
```

(`my_queue_status`'s own `progress_status` column is 17a's job per the brief — nothing further
needed there from this doc.)

## 5. Client-side note: errors are now actually shown to users

Unrelated to schema, but worth knowing before testing 17b.7's scenarios: `HandleExceptionAsync`
across this whole app was a no-op before this PR — it only wrote to the debug log, so **no
ViewModel anywhere surfaced errors to the user**, including the pre-existing "someone else just
booked that slot" conflict message on `BusinessDetailPage`, which has been silently swallowed
since it was written. This PR adds real popups, scoped to just the two ViewModels this step
touches (`OperatorQueuePageViewModel`, which already had a popup service on hand;
`BusinessDetailPageViewModel`, which now takes one) — not an app-wide fix, since that would mean
threading a popup dependency through every ViewModel in the app, well beyond this step's scope.
`BookingAgendaPageViewModel`'s errors (including its own new `SaveProgressAsync`) are unchanged —
still silent, consistent with the rest of the app outside these two screens.
