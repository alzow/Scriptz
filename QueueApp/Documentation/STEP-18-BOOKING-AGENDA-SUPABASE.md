# Step 18 — Supabase changes the booking agenda needs

## Read this first

`BookingAgendaPage` was rebuilt to the agenda spec in this change. Everything on it works against the
schema as `SUPABASE-SCHEMA-VERIFIED.md` describes it **except the things below**, which are listed
in the order they hurt. Nothing here is speculative UI: the client already sends these calls, so
applying §1 turns one refusing button into a working one, and §3 — decided and written out in full,
including the read-policy tightening it depends on — stops the operator seeing "Customer" where a
name should be and gives them back the call button on bookings customers made themselves.

Four things turned out **not** to need SQL:

- **No-show and move** go through PostgREST `PATCH /bookings`, which the existing `bookings`
  "owner or self manage" UPDATE policy already permits. No new RPC.
- **Operator-created bookings** go through `POST /bookings` with `status = 'confirmed'` and a null
  `customer_id`, which the existing insert policy already permits via `is_business_owner`.
  `create_booking` is the customer path and needs a real `customer_id` a phone booking hasn't got.
- **Additional details** (vehicle registration, what's actually wrong) use the `bookings.note`
  column, which already exists, and `create_booking` / `create_booking_any` already accept it as
  `p_note`. Captured on the review step — which the shop now walks too, since "Add a booking"
  pushes the customer's booking flow in operator mode instead of a sheet of its own — and read
  back to the operator in the booking actions sheet.
- **Cancellation reasons** live in `bookings.details` under `cancellation_reason`, which stays a
  jsonb key rather than becoming a column: unlike a name or a number, nothing queries or joins on
  it. `cancel_booking`
  takes no reason, so the reason is PATCHed onto `details` first and the RPC called after. The
  PATCH replaces the whole jsonb value, so `BookingDetails.WithCancellationReason` carries the
  existing keys across.

---

## 1. `booking_status` has nowhere to put a finished booking — **blocking**

§1g of the schema doc confirms `booking_status` is a real enum but never captured its labels. Every
label the app has ever sent is one of `pending` / `confirmed` / `cancelled` / `completed`. One more
is needed, and one is now optional:

- **`no_show`** — the sheet offers "Didn't show up". Today the only honest place to put that is
  `cancelled`, which loses the distinction that matters for the day's revenue figure. This is the
  one that still blocks.
- **`in_progress`** — no longer written by anything. The start action and the now/next card were
  both removed rather than left decorative, so nothing depends on this value. The agenda still
  *renders* an in-progress booking if the data ever carries one; it just never writes one.

Run this first to see what's actually there:

```sql
select e.enumlabel
from pg_type t join pg_enum e on t.oid = e.enumtypid
where t.typname = 'booking_status'
order by e.enumsortorder;
```

Then add whatever is missing:

```sql
alter type booking_status add value if not exists 'no_show';
-- Only needed if the start / in-chair flow is brought back:
-- alter type booking_status add value if not exists 'in_progress';
```

`add value` can't run inside a transaction block in older Postgres — run these as standalone
statements if the migration tool wraps everything.

**Until this is applied:** "Didn't show up" PATCHes a status Postgres will reject, and the error
surfaces to the operator as a popup rather than failing silently. Cancel still works, and it is the
only destructive action offered outside the booking window. Nothing else on the page breaks — no
query filters on either label, deliberately, because PostgREST rejects a whole query for an enum
label it can't parse.

## 2. `bookings` has no "actually began" timestamp — no longer blocking

`bookings.starts_at` is *scheduled*, not *began*. Queue mode has `queue_entries.serving_at`; booking
mode has no equivalent.

Nothing needs it any more. The elapsed counter it was for went with the now/next card, and the sheet
now decides what to offer from the scheduled window instead: the customer update field and
"Didn't show up" appear only while `now` is inside `[starts_at, ends_at)`, and "Mark as done" only
once that window has opened. That is honest about scheduled time rather than pretending to know
actual time.

If the start flow comes back, this is what it needs:

```sql
alter table bookings add column if not exists started_at timestamptz;
alter table bookings add column if not exists completed_at timestamptz;
```

## 3. An operator cannot read their own customers' names or numbers — **apply this**

`profiles` has exactly two policies, self read (`auth.uid() = id`) and self update, and `bookings`
has no name or phone of its own. So the agenda's embedded `customer:profiles(display_name,phone)`
returns nothing for any booking the shop didn't create itself, which is why every customer-made row
reads "Customer" and why the call button on the booking actions sheet never appears on one.

**Decision taken: denormalise onto `bookings`, the way `queue_entries` already does, and tighten
that table's read policy in the same migration.** The client is already written for it — it reads
`customer_name` / `customer_phone` first, then the profile embed, then the legacy `details` keys —
so applying this needs no further code change.

Run all four steps together. Step 4 is not optional: without it these columns are readable by every
signed-in user, which is strictly worse than where the data started.

### 1. The columns

```sql
alter table bookings add column if not exists customer_name text;
alter table bookings add column if not exists customer_phone text;
```

### 2. Backfill what already exists

```sql
update bookings b
set customer_name  = coalesce(b.customer_name,  p.display_name),
    customer_phone = coalesce(b.customer_phone, p.phone)
from profiles p
where p.id = b.customer_id
  and (b.customer_name is null or b.customer_phone is null);

-- Bookings the shop took by phone kept the name in details before this migration.
update bookings
set customer_name  = coalesce(customer_name,  details->>'customer_name'),
    customer_phone = coalesce(customer_phone, details->>'customer_phone')
where customer_id is null
  and details is not null;
```

### 3. Keep them filled, without touching `create_booking`

A trigger rather than an edit to `create_booking` / `create_booking_any`, because this repo has
never had those functions' source (STEP-17-SUPABASE.md §3 makes the same point) and because a
trigger also covers the operator's direct insert and anything added later.

```sql
create or replace function fill_booking_customer_snapshot()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
begin
  if new.customer_id is not null then
    select coalesce(new.customer_name, p.display_name),
           coalesce(new.customer_phone, p.phone)
      into new.customer_name, new.customer_phone
    from profiles p
    where p.id = new.customer_id;
  end if;

  return new;
end;
$$;

drop trigger if exists bookings_fill_customer_snapshot on bookings;

create trigger bookings_fill_customer_snapshot
before insert on bookings
for each row execute function fill_booking_customer_snapshot();
```

`security definer` is what lets it read `profiles` past that table's self-read policy. It only ever
reads the profile of the `customer_id` already on the row being inserted.

It is a snapshot, not a live join: a customer who changes their number later does not change the
number on a booking already made. That is usually what a business wants — the number they were given
when the booking was taken — but it is a real difference from option B and worth knowing.

### 4. Tighten the read policy — do not skip this

`bookings` currently has `public read` with `qual = true` (§1b of the schema doc). Leaving that in
place would publish every customer's phone number to every signed-in user.

Find the policy's real name first, since this doc only knows it by description:

```sql
select policyname, cmd, qual from pg_policies
where schemaname = 'public' and tablename = 'bookings';
```

Then replace the SELECT one:

```sql
drop policy "<the public read policy's name>" on bookings;

create policy "owner or self read" on bookings for select
using (is_business_owner(business_id) or auth.uid() = customer_id);
```

Every booking read the app makes is already scoped to one of those two: the agenda and the requests
banner filter by `business_id` on a business the user owns, and the customer's upcoming, history and
per-business lists filter by their own `customer_id`. Slot generation goes through
`get_available_slots` / `get_available_slots_any`, which are `security definer` and unaffected by
this. Nothing in the app reads a booking belonging to someone else.

### Not chosen, and why

- **A `profiles` SELECT policy for businesses** (`exists (select 1 from bookings where
  bookings.customer_id = profiles.id and is_business_owner(bookings.business_id))`) keeps the phone
  in `profiles` and stays live rather than snapshotted. Narrower exposure, but a wider read surface
  on `profiles` and a join on every agenda row. Reasonable; not what was picked.
- **Copying the details client-side at booking time** needs no migration but writes a phone number
  into the same row with none of the backend control, and the copy drifts from the profile.

## 4. Decided while building, not a gap

- **Revenue excludes cancelled and no-show bookings.** `BookingStatuses.CountsTowardsRevenue` is the
  single place that decision lives. A day figure that counts money nobody paid is worse than no
  figure at all.
- **`bookings_no_overlap` still knows nothing about `availability_blocks`.** Unchanged, and left
  that way: the requests banner checks the overlap in the query and warns, and the block sheet names
  the bookings a new block would strand. Widening the gist constraint to cover blocks would also
  make it impossible to block time over an existing booking at all, which is not what an operator
  wants when the bay floods.
- **Declining and cancelling now both ask for a reason** and store it in `details`. The customer
  sees it on their history row. It is a free-text prompt rather than the three-option picker the
  spec floated, because the reasons that actually come up ("bay flooded", "parts didn't arrive")
  don't fit three options.
