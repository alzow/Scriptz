# Location — required Supabase changes

This branch (`location-services`, built off `ui-enhancements`) adds real distance to the Browse
dashboard — a "Mr Delivery" feel: a location bar the customer can tap to (re)resolve their device
location, and businesses sorted nearest-first with a live "1.2 km" badge, instead of the static
"1.2 km" placeholder the previous PR shipped (that field existed on the model but nothing computed
it — `nearby_business_summary` didn't return `distance_km` at all).

**This supersedes the `nearby_business_summary` SQL in
`README-UI-ENHANCEMENTS-SUPABASE.md` §2** — if you haven't run that one yet, skip straight to the
version below; if you already did, you need to `drop` and recreate it (see why below).

Same caveat as before: I don't have access to your live Supabase project, and I don't have
visibility into your actual `operator_avg_minutes`/`business_queue_summary` source — review this
against those before running it.

## What I deliberately did *not* build

- **No saved-addresses table** (no "Home"/"Work"/"Other" like a full delivery app). The customer's
  location is device GPS only, refreshed on demand, cached locally on-device
  (`SecureStorage`) — **never sent to or stored in Supabase**. Each `nearby_business_summary`
  call carries `p_customer_lat`/`p_customer_lng` fresh, computes distance, and forgets it. This
  was a deliberate privacy-over-features call for v1: no location history to worry about
  retaining or securing server-side. A `customer_addresses(customer_id, label, address, lat, lng,
  is_default)` table is the natural extension if you want a real address book later — happy to
  build it, but wanted to flag it as a decision rather than silently add a table that persists
  customer whereabouts.
- **No delivery radius / "outside our area" cutoff.** Results are sorted by distance, not filtered
  by it — the app is single-suburb (`p_suburb` still gates the query) so a radius cutoff isn't
  meaningful yet. Worth adding once businesses span more than one suburb.
- **No manual address entry / map pin-drop** for either customers or business owners — both sides
  use "use my current location" (device GPS) only. A business owner sets their shop's location by
  standing in it and tapping a button (`BusinessLocationPage`, linked from Business Settings). Manual
  entry is a reasonable v2 if an owner can't be on-site when setting this up (e.g. remote setup).

## 1. `haversine_km(lat1, lng1, lat2, lng2)` — distance helper

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

## 2. `nearby_business_summary` — now with distance

PostgreSQL won't let `create or replace function` change a table-returning function's output
columns, so this needs a `drop` first if you already ran the 2-argument version from the earlier
PR:

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
    case when b.mode = 'booking' then (
      select min(starts_at) from bookings bk
      where bk.business_id = b.id and bk.status = 'pending' and bk.starts_at > now()
    ) end
  from businesses b
  left join lateral (
    select
      count(*) filter (where qe.status = 'waiting') as waiting_count,
      count(distinct qe.operator_id) filter (where qe.status = 'serving') as operators_working_count,
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
customerLongitude)` (`Services/Api/Business/BusinessService.cs`) — the two new params are optional
(`default null`), so the app degrades to the old wait-time-only ordering whenever the customer's
location hasn't resolved yet (permission denied, GPS off, first launch before the fix lands).

## 3. Business location — no new SQL, just the existing owner-update policy

`BusinessLocationPage` (new, linked from Business Settings → Location) lets an owner set their
shop's `latitude`/`longitude` via `IBusinessService.UpdateLocationAsync`, which is a plain `PATCH
/businesses?id=eq.<id>` — same shape as the existing `HeartbeatAsync`. This is already covered by
the `businesses` "owner update" RLS policy (`auth.uid() = owner_id`, per the schema doc §3) — no
migration needed, it's exactly the same write path that already works for everything else on that
table.

## 4. Client-side location — no Supabase involvement at all

`ILocationService` (`Services/Location/`) wraps `Geolocation.GetLocationAsync` +
`Geocoding.GetPlacemarksAsync` (both on-device MAUI Essentials APIs — no external geocoding API
key needed) and caches the last fix in `SecureStorage`. It's registered as the real implementation
even in `USE_STUBS` builds, since it's a device capability, not a backend call — it already fails
soft (returns `null`) if permission is denied or no fix is available within ~12s, and the app falls
back to the existing suburb-only browsing in that case. `StubBusinessService` mirrors the real
`nearby_business_summary`'s distance math client-side (same haversine formula) against fixed sample
coordinates, so the feature is fully demoable without a real device fix or a Supabase project.

Platform permission declarations were added for this: `ACCESS_COARSE_LOCATION` /
`ACCESS_FINE_LOCATION` on Android, `NSLocationWhenInUseUsageDescription` on iOS/MacCatalyst, and
the `location` `DeviceCapability` on Windows.
