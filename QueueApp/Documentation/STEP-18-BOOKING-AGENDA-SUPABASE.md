# Step 18 — Supabase changes the booking agenda needs

## Read this first

`BookingAgendaPage` was rebuilt to the agenda spec in this change. Everything on it works against the
schema as `SUPABASE-SCHEMA-VERIFIED.md` describes it **except the three things below**, which are
listed in the order they hurt. Nothing here is speculative UI: the client already sends these calls,
so applying §1 turns two decorative buttons into working ones and applying §3 stops the operator
seeing "Customer" where a name should be.

Two things the spec worried about turned out **not** to need SQL:

- **Start, no-show and move** go through PostgREST `PATCH /bookings`, which the existing
  `bookings` "owner or self manage" UPDATE policy already permits. No new RPC.
- **Operator-created bookings** go through `POST /bookings` with `status = 'confirmed'` and a null
  `customer_id`, which the existing insert policy already permits via `is_business_owner`.
  `create_booking` is the customer path and needs a real `customer_id` a phone booking hasn't got.

---

## 1. `booking_status` has nowhere to put a finished booking — **blocking**

§1g of the schema doc confirms `booking_status` is a real enum but never captured its labels. Every
label the app has ever sent is one of `pending` / `confirmed` / `cancelled` / `completed`. The agenda
needs two more:

- **`in_progress`** — the card offers `Done` and the sheet offers "They've arrived — start", and
  without a value between confirmed and completed there is nowhere to record that someone is in the
  chair right now.
- **`no_show`** — the sheet offers "Didn't show up". Today the only honest place to put that is
  `cancelled`, which loses the distinction that matters for the day's revenue figure.

Run this first to see what's actually there:

```sql
select e.enumlabel
from pg_type t join pg_enum e on t.oid = e.enumtypid
where t.typname = 'booking_status'
order by e.enumsortorder;
```

Then add whatever is missing:

```sql
alter type booking_status add value if not exists 'in_progress';
alter type booking_status add value if not exists 'no_show';
```

`add value` can't run inside a transaction block in older Postgres — run these as standalone
statements if the migration tool wraps everything.

**Until this is applied:** the `Done` and start actions PATCH a status Postgres will reject, and the
error surfaces to the operator as a popup rather than failing silently. Nothing else on the page
breaks — no query filters on either label, deliberately, because PostgREST rejects a whole query for
an enum label it can't parse.

## 2. `bookings` has no "actually began" timestamp

`bookings.starts_at` is *scheduled*, not *began*. Queue mode has `queue_entries.serving_at`; booking
mode has no equivalent, so `IN CHAIR NOW` can only count against the schedule — which is wrong from
the moment someone starts late.

```sql
alter table bookings add column if not exists started_at timestamptz;
```

The agenda query selects `*` rather than a column list precisely so it keeps working before this
lands and picks the column up by itself afterwards. `AgendaBookingResponse.IsInProgress` also treats
"has a `started_at` and is still `confirmed`" as in progress, so this column alone makes the card
work even if §1 is applied later.

Worth considering alongside it, though nothing depends on it yet:

```sql
alter table bookings add column if not exists completed_at timestamptz;
```

## 3. An operator cannot read their own customers' names

This one isn't in the spec's gap list and is worth a decision before launch.

`profiles` has exactly two policies — self read (`auth.uid() = id`) and self update. `bookings` has
no `customer_name` column, unlike `queue_entries`, which denormalises it precisely so a business can
see who is waiting. So the agenda's embedded `customer:profiles(display_name)` returns null for
every booking the shop didn't create itself, and **every row reads "Customer"**.

The agenda mitigates what it can: bookings the operator took over the phone carry the name in
`details`, which the owner can always read. Bookings customers made through the app cannot be
mitigated client-side.

Two ways out, and they're a genuine choice rather than a fix to apply blind — the same "public read"
question §1b of the schema doc raises, pointing the other way:

- **A — denormalise, like the queue does.** `alter table bookings add column customer_name text;` and
  have `create_booking` fill it from the customer's profile. Nothing new becomes readable that
  `queue_entries` doesn't already expose, and the agenda needs no policy change.
- **B — let a business read the profiles of people booked with it.** A `profiles` SELECT policy along
  the lines of `exists (select 1 from bookings where bookings.customer_id = profiles.id and
  is_business_owner(bookings.business_id))`. Narrower in what it exposes, wider in what it lets a
  business query.

A is the smaller change and matches what queue mode already does.

## 4. Decided while building, not a gap

- **Revenue excludes cancelled and no-show bookings.** `BookingStatuses.CountsTowardsRevenue` is the
  single place that decision lives. A day figure that counts money nobody paid is worse than no
  figure at all.
- **`bookings_no_overlap` still knows nothing about `availability_blocks`.** Unchanged, and left
  that way: the requests banner checks the overlap in the query and warns, and the block sheet names
  the bookings a new block would strand. Widening the gist constraint to cover blocks would also
  make it impossible to block time over an existing booking at all, which is not what an operator
  wants when the bay floods.
- **Declining sends no reason.** Ships degraded, as the spec anticipated. The customer gets a
  rejection with no explanation. A three-option picker (fully booked / closed that day / other)
  would need somewhere to put the reason — `bookings.note` is the obvious candidate.
