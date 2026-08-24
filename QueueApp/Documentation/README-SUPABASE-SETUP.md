# Browse dashboard + location — required Supabase changes

Single runbook for everything needed to make the redesigned Browse dashboard (`ui-enhancements`)
and location/distance (`location-services`) work against your real Supabase project. `USE_STUBS`
builds work today with none of this — the app's C# side (models, services, view models, XAML) is
already fully wired up to call everything below, it just needs these to exist server-side.

I don't have access to your live Supabase project, and no visibility into the literal SQL source
of your existing `my_queue_status`, `business_queue_summary`, `queue_entry_wait_minutes` and
`operator_avg_minutes` functions — only their documented behavior
(`Documentation/SUPABASE-SCHEMA-VERIFIED.md` §4). Everything below is a proposed, self-contained
implementation in the same spirit (`security definer`, `auth.uid()` scoping, the `count(*) >= 3` /
`greatest(avg, 1)` guard pattern already used elsewhere) — **review it against your actual helper
functions before running it**, and prefer calling your existing functions internally instead of
duplicating logic where they already do what's needed.

## Quick start

Run these two blocks, in order, in the Supabase SQL editor. That's the entire migration — nothing
else in this document needs SQL run (see the "no migration needed" sections below for why).

1. §1 — `my_active_queue_entry()`
2. §2 — `haversine_km()` then `nearby_business_summary()` (§2 includes a `drop function` line so
   it's safe to run whether or not you've created an earlier version of this function before)

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

## 2. `nearby_business_summary(category, suburb, customer_lat, customer_lng)` — "Open now near you"

**Why it's needed:** `business_queue_summary(business_id)` returns a live wait breakdown for *one*
business (already used on the business detail page). The Browse list needs the same kind of live
number for *every* business in a category in a single call — plus, once the customer's device
location is known, their distance from each one — so the screen isn't firing one queue-summary
call per card.

### 2a. `haversine_km` — distance helper

Plain-SQL haversine, no PostGIS/`earthdistance` extension dependency (didn't want to assume one's
enabled on your project). Fine at this scale (one suburb, tens of businesses) — a spatial index
only matters once you're doing this across a much larger area/business count.

```sql
create or replace function haversine_km(lat1 float8, lng1 float8, lat2 float8, lng2 float8)
returns float8
language sql
immutable
parallel safe
as $$
  select 6371 * 2 * asin(sqrt(
    sin(radians(lat2 - lat1) / 2) ^ 2 +
    cos(radians(lat1)) * cos(radians(lat2)) *
    sin(radians(lng2 - lng1) / 2) ^ 2
  ));
$$;

grant execute on function haversine_km(float8, float8, float8, float8) to authenticated;
```

### 2b. `nearby_business_summary`

PostgreSQL won't let `create or replace function` change a table-returning function's output
columns, so this drops any earlier version first — safe to run even if you've never created this
function before (`drop ... if exists` is a no-op in that case):

```sql
drop function if exists nearby_business_summary(text, text);

create or replace function nearby_business_summary(
  p_category text,
  p_suburb text default 'Lenasia',
  p_customer_lat float8 default null,
  p_customer_lng float8 default null
)
returns table (
  id uuid,
  name text,
  category text,
  mode text,
  address text,
  latitude float8,
  longitude float8,
  distance_km float8,
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
    case
      when p_customer_lat is not null and p_customer_lng is not null
       and b.latitude is not null and b.longitude is not null
      then haversine_km(p_customer_lat, p_customer_lng, b.latitude, b.longitude)
    end as distance_km,
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
    and (p_category is null or b.category::text = p_category)
  order by distance_km nulls last, b.name;
$$;

grant execute on function nearby_business_summary(text, text, float8, float8) to authenticated;
```

Called from `IBusinessService.GetBrowseBusinessesAsync(category, suburb, customerLatitude,
customerLongitude)` (`Services/Api/Business/BusinessService.cs`) — the two location params are
optional (`default null`), so the app degrades to wait-time-only ordering whenever the customer's
location hasn't resolved yet (permission denied, GPS off, first launch before the fix lands).

## 3. Everything else — no migration needed

Nothing below this line requires SQL. Listed so you can see the full picture of what the two
branches touch without wondering whether something got missed.

- **`businesses.latitude` / `longitude`** already existed in your schema (§1c of the schema doc),
  just dormant/empty. `location-services` adds the actual write path: `BusinessLocationPage`
  (Business Settings → Location) lets an owner set them via "use my current location", through a
  new `IBusinessService.UpdateLocationAsync` — a plain `PATCH /businesses?id=eq.<id>`, same shape
  as the existing `HeartbeatAsync`. Already covered by the `businesses` "owner update" RLS policy
  (`auth.uid() = owner_id`, schema doc §3) — same write path everything else on that table
  already uses.
- **The live-queue hero card is realtime**, not just refresh-on-load: `CategoryPickerPageViewModel`
  subscribes through `IQueueRealtimeService` (the same Postgres Changes mechanism
  `BusinessDetailPage` already uses), scoped dynamically — `customer_id`-filtered while idle (so
  joining a queue from any screen is picked up), switching to `business_id`-filtered once queued
  (the same scope `BusinessDetailPage` uses, so position/wait updates when people *ahead* of the
  customer are served or leave, not just when their own row changes). This reuses the existing
  `queue_entries` Realtime publication and its public-read RLS policy (§1b) — nothing new to
  enable. Worth a quick check in the Supabase dashboard (Database → Replication) that
  `queue_entries` is still in the `supabase_realtime` publication, since I can't verify that from
  here.
- **"You go here a lot"** groups `GetMyVisitsAsync` results client-side — the only change was
  widening the existing `visits` embedded select to include `business:businesses(id,name)` instead
  of just `name`, so a tap can navigate to the business. `visits` (self-read) and `businesses`
  (public read active) RLS already allow this.
- **Upcoming bookings** uses `GetMyUpcomingBookingsAsync`, unchanged from before either branch.
- **Client-side location** (`ILocationService`, `Services/Location/`) wraps on-device
  `Geolocation`/`Geocoding` APIs (no external geocoding API key needed) and caches the last fix in
  `SecureStorage` — **customer location is never sent to or stored in Supabase**, only passed
  fresh as `p_customer_lat`/`p_customer_lng` per request. Platform permission declarations
  (`ACCESS_COARSE_LOCATION`/`ACCESS_FINE_LOCATION` on Android,
  `NSLocationWhenInUseUsageDescription` on iOS/MacCatalyst, the Windows `location` capability) are
  already in the app project — nothing to configure in Supabase for this either.

## 4. Deliberately out of scope

- **Travel-time-aware "leave at HH:MM"**: the hero card's ring shows a wait-time countdown only —
  not adjusted for how long it'd take the customer to get there. That needs either live routing or
  a per-customer "usual travel time to this business" setting, neither of which exists yet. A
  reasonable v2 shape would be a `customer_business_travel_minutes(customer_id, business_id,
  minutes)` table, but that's a product decision (and a new table) worth making deliberately.
- **Category catalog expansion**: the original mockup's carousel showed many more categories
  (salons, car wash, doctors, dentists, pharmacy, nails, vets, tyres, laundry, optical, food) than
  the app currently supports. `CategoryCatalog` (`Features/CategoryPicker/CategoryCatalog.cs`)
  still only lists `barber` (active) and `carwash` (disabled, matching what already existed) — the
  carousel is fully data-driven off that list, so adding a category later is a one-line change plus
  a `business_category` enum value, not a UI change. I didn't invent categories or enum values
  since that's a product/schema decision.
- **No saved-addresses table** (no "Home"/"Work"/"Other" like a full delivery app) — customer
  location is live GPS only, cached on-device, never persisted server-side. Deliberate
  privacy-over-features call for v1: no location history to worry about retaining or securing.
  A `customer_addresses(customer_id, label, address, lat, lng, is_default)` table is the natural
  extension if you want a real address book later.
- **No delivery radius / "outside our area" cutoff** — results are sorted by distance, not
  filtered by it. The app is still single-suburb (`p_suburb` gates the query), so a radius cutoff
  isn't meaningful yet; worth adding once businesses span more than one suburb.
- **No manual address entry / map pin-drop** for either customers or business owners — both sides
  use "use my current location" only. A business owner sets their shop's location by standing in
  it and tapping a button. Manual entry is a reasonable v2 if an owner can't be on-site when
  setting this up.
