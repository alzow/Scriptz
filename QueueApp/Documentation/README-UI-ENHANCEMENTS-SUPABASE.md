# Browse dashboard — required Supabase changes

This branch (`ui-enhancements`) redesigns the customer home screen — the **Browse** tab
(`CategoryPickerPage`, previously just a flat list of two categories) into a real dashboard: a
live "you're in the queue" hero, a category carousel, upcoming bookings, nearby businesses with
live wait times, and a "you go here a lot" strip. See the mockup you supplied for the target
design; the C# side (models, services, view models, XAML content views) is fully wired up to
call the endpoints below — **it just needs these two RPCs to exist in your Supabase project**
before the real (non-stub) build will return live data. `USE_STUBS` builds work today without
any of this, using in-memory sample data (see `Services/Stubs/`), so you can review the UI
immediately.

I don't have visibility into the literal SQL source of your existing `my_queue_status`,
`business_queue_summary`, `queue_entry_wait_minutes` and `operator_avg_minutes` functions — only
their documented behavior (`Documentation/SUPABASE-SCHEMA-VERIFIED.md` §4). The SQL below is a
proposed, self-contained implementation in the same spirit (`security definer`, `auth.uid()`
scoping, the `count(*) >= 3` / `greatest(avg, 1)` guard pattern already used elsewhere) — **review
it against your actual helper functions before running it**, and prefer calling your existing
functions internally instead of duplicating logic where they already do what's needed.

## 1. `my_active_queue_entry()` — powers the live-queue hero card

**Why it's needed:** `my_queue_status(business_id)` requires knowing which business to check.
The Browse dashboard doesn't know that ahead of time — it needs "is this customer queued
*anywhere*, and where?" across all businesses. This is the cross-business equivalent.

Called from `IQueueService.GetMyActiveEntryAsync()` (`Services/Api/Queue/QueueService.cs`), which
hits `POST /rpc/my_active_queue_entry` with no body.

```sql
create or replace function my_active_queue_entry()
returns table (
  entry_id uuid,
  business_id uuid,
  business_name text,
  business_latitude float8,
  business_longitude float8,
  operator_id uuid,
  operator_name text,
  queue_position int,
  status text,
  joined_at timestamptz,
  wait_minutes numeric
)
language sql
security definer
set search_path = public
as $$
  with mine as (
    select qe.*
    from queue_entries qe
    where qe.customer_id = auth.uid()
      and qe.status in ('waiting', 'serving')
    order by qe.joined_at desc
    limit 1
  ),
  ranked as (
    select
      qe.id,
      qe.business_id,
      qe.operator_id,
      qe.joined_at,
      row_number() over (
        partition by qe.business_id, qe.operator_id
        order by qe.joined_at
      ) as position
    from queue_entries qe
    where qe.status in ('waiting', 'serving')
      and qe.business_id = (select business_id from mine)
      and qe.operator_id is not distinct from (select operator_id from mine)
  )
  select
    m.id,
    m.business_id,
    b.name,
    b.latitude,
    b.longitude,
    m.operator_id,
    coalesce(o.display_name, 'Any available'),
    r.position::int,
    m.status,
    m.joined_at,
    -- Reuse your existing per-entry wait estimate function instead of this fallback if it
    -- already does something smarter (operator_avg_minutes-based, etc).
    queue_entry_wait_minutes(m.id)
  from mine m
  join businesses b on b.id = m.business_id
  left join operators o on o.id = m.operator_id
  join ranked r on r.id = m.id;
$$;

grant execute on function my_active_queue_entry() to authenticated;
```

## 2. `nearby_business_summary(p_category, p_suburb)` — powers "Open now near you"

**Why it's needed:** `business_queue_summary(business_id)` returns a live wait breakdown for
*one* business (already used on the business detail page). The Browse list needs the same kind
of live number for *every* business in a category in a single call, so the screen doesn't fire
N queue-summary requests for N cards.

Called from `IBusinessService.GetBrowseBusinessesAsync(category, suburb)`
(`Services/Api/Business/BusinessService.cs`), which hits `POST /rpc/nearby_business_summary`.

```sql
create or replace function nearby_business_summary(p_category text, p_suburb text default 'Lenasia')
returns table (
  id uuid,
  name text,
  category text,
  mode text,
  address text,
  latitude float8,
  longitude float8,
  is_active bool,
  last_seen_at timestamptz,
  waiting_count int,
  operators_working_count int,
  avg_wait_minutes numeric,
  next_slot_starts_at timestamptz
)
language sql
security definer
set search_path = public
as $$
  select
    b.id,
    b.name,
    b.category::text,
    b.mode::text,
    b.address,
    b.latitude,
    b.longitude,
    b.is_active,
    b.last_seen_at,
    coalesce(q.waiting_count, 0)::int,
    coalesce(q.operators_working_count, 0)::int,
    q.avg_wait_minutes,
    -- Booking-mode businesses: soonest open slot today or later. Adjust to match however
    -- availability_blocks / operator_availability / bookings are combined elsewhere.
    case when b.mode = 'booking' then (
      select min(starts_at) from bookings bk
      where bk.business_id = b.id and bk.status = 'pending' and bk.starts_at > now()
    ) end
  from businesses b
  left join lateral (
    select
      count(*) filter (where qe.status = 'waiting') as waiting_count,
      count(distinct qe.operator_id) filter (where qe.status = 'serving') as operators_working_count,
      -- Same guard as operator_avg_minutes: only trust the average once there's enough
      -- history, otherwise fall back to a flat per-person estimate.
      case when count(*) filter (where qe.status = 'serving') >= 3
        then avg(operator_avg_minutes(qe.operator_id))
        else greatest(count(*) filter (where qe.status = 'waiting') * 10, 1)
      end as avg_wait_minutes
    from queue_entries qe
    where qe.business_id = b.id and qe.status in ('waiting', 'serving')
  ) q on b.mode = 'queue'
  where b.is_active = true
    and b.suburb = p_suburb
    and (p_category is null or b.category::text = p_category);
$$;

grant execute on function nearby_business_summary(text, text) to authenticated;
```

## 3. Live updates — no new SQL, but worth confirming

The hero card is realtime now, not just refresh-on-load: `CategoryPickerPageViewModel` subscribes
through `IQueueRealtimeService` (the same Postgres Changes mechanism `BusinessDetailPage` already
uses), scoped dynamically —

- **Idle** (not queued anywhere): subscribed to `queue_entries` filtered on `customer_id = <me>`,
  so joining a queue from any screen is picked up immediately.
- **Queued somewhere**: switches to `business_id = <that business>`, the same scope
  `BusinessDetailPage` uses — deliberately business-wide rather than customer-only, so the
  position/wait shown updates when people *ahead* of the customer are served or leave, not only
  when the customer's own row changes.

It re-evaluates which scope it needs after every change and only tears down/reopens the socket
when the scope actually flips (idle ↔ queued), not on every event.

This reuses the existing `queue_entries` Realtime publication and RLS — `queue_entries` already
has a public read policy (§1b of the schema doc), which is what lets `BusinessDetailPage`'s
existing business-scoped subscription work today, so the customer-scoped one needs nothing extra
enabled. Worth a quick check in the Supabase dashboard (Database → Replication) that `queue_entries`
is still in the `supabase_realtime` publication, since I can't verify that from here.

`IQueueRealtimeService.SubscribeAsync` changed shape to support this: it now takes an explicit
`(filterColumn, filterValue)` pair instead of a hardcoded `business_id`. All four existing
call sites (`BusinessDetailPage` ×2, `OperatorQueuePage`, `BookingAgendaPage`) were updated to
pass `"business_id"` explicitly — behavior for those screens is unchanged.

## 4. Already-covered by existing RLS — no migration needed

- **`businesses.latitude` / `longitude`** already exist (§1c of the schema doc) and are read by
  the two functions above, and by `BusinessResponse`/`BrowseBusinessSummaryResponse` on the C#
  side, to power the hero card's **Directions** button (`Map.Default.OpenAsync`). They're
  dormant/empty per the schema doc — the button degrades to an alert ("hasn't added a map
  location yet") until businesses actually populate these columns. No SQL required, just data.
- **"You go here a lot"** (`FrequentBusinessListView`) groups `GetMyVisitsAsync` results
  client-side — the only change was widening the existing `visits` embedded select to include
  `business:businesses(id,name)` instead of just `name`, so a tap can navigate to the business.
  `visits` (self-read) and `businesses` (public read active) RLS already allow this; no new
  policy needed.
- **Upcoming bookings** (`UpcomingBookingsListView`) uses `GetMyUpcomingBookingsAsync`, which
  already existed and is unchanged.

## 5. Deliberately out of scope for this PR

- **Travel-time-aware "leave at HH:MM"**: the mockup's ring shows a wait-time countdown only —
  not adjusted for how long it'd take the customer to get there. That needs either geo distance
  or a per-customer "usual travel time to this business" setting, which doesn't exist yet. A
  reasonable v2 shape would be a `customer_business_travel_minutes(customer_id, business_id,
  minutes)` table, but that's a product decision (and a new table) worth making deliberately
  rather than bundling into this UI pass.
- **Category catalog expansion**: the mockup's carousel shows many more categories (salons, car
  wash, doctors, dentists, pharmacy, nails, vets, tyres, laundry, optical, food) than the app
  currently supports. `CategoryCatalog` (`Features/CategoryPicker/CategoryCatalog.cs`) still only
  lists `barber` (active) and `carwash` (disabled, matching what already existed before this
  branch) — the carousel is fully data-driven off that list, so adding a category later is a
  one-line change plus a `business_category` enum value, not a UI change. I didn't invent new
  categories or enum values since that's a product/schema decision, not implied by "redesign the
  dashboard."
