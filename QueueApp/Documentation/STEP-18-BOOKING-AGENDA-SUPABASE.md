# Step 18 — Supabase changes the booking agenda needs

## Read this first

`BookingAgendaPage` was rebuilt to the agenda spec in this change. Everything on it works against the
schema as `SUPABASE-SCHEMA-VERIFIED.md` describes it **except the things below**, which are listed
in the order they hurt. Nothing here is speculative UI: the client already sends these calls, so
applying §1 turns one refusing button into a working one, and applying §3 stops the operator seeing
"Customer" where a name should be and gives them back the call button on bookings customers made
themselves.

Four things turned out **not** to need SQL:

- **No-show and move** go through PostgREST `PATCH /bookings`, which the existing `bookings`
  "owner or self manage" UPDATE policy already permits. No new RPC.
- **Operator-created bookings** go through `POST /bookings` with `status = 'confirmed'` and a null
  `customer_id`, which the existing insert policy already permits via `is_business_owner`.
  `create_booking` is the customer path and needs a real `customer_id` a phone booking hasn't got.
- **Additional details** (vehicle registration, what's actually wrong) use the `bookings.note`
  column, which already exists, and `create_booking` / `create_booking_any` already accept it as
  `p_note`. Captured on the customer's review step and on the operator's add-booking sheet, and
  read back to the operator in the booking actions sheet.
- **Cancellation reasons** live in `bookings.details` under `cancellation_reason`. `cancel_booking`
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

## 3. An operator cannot read their own customers' names or numbers

This one isn't in the spec's gap list and is the most visible thing still missing.

`profiles` has exactly two policies — self read (`auth.uid() = id`) and self update. `bookings` has
no `customer_name` or `customer_phone` column, unlike `queue_entries`, which denormalises the name
precisely so a business can see who is waiting. So the agenda's embedded
`customer:profiles(display_name,phone)` returns nothing for every booking the shop didn't create
itself, and two things follow:

- **every customer-made row reads "Customer"**, and
- **the call button on the booking actions sheet never appears** for a customer-made booking, only
  for one the operator took over the phone.

Neither is a client bug. The client already reads both sources — `Customer?.Phone ?? Details?.
CustomerPhone`, same for the name — so whichever fix below is applied, the agenda picks it up with
no further code change.

Three ways out, and it's a genuine choice:

- **A — denormalise, like the queue does.**

  ```sql
  alter table bookings add column if not exists customer_name text;
  alter table bookings add column if not exists customer_phone text;
  ```

  and have `create_booking` / `create_booking_any` fill them from the customer's profile. This is
  the smallest change and it matches what queue mode already does. Note what it means though:
  `bookings` has `public read` with `qual = true` (§1b of the schema doc), so a phone number in a
  `bookings` column is readable by **any signed-in user**, not just the business. Worth pairing with
  tightening that read policy.

- **B — let a business read the profiles of people booked with it.**

  ```sql
  create policy "business reads booked customers" on profiles for select
  using (exists (
    select 1 from bookings
    where bookings.customer_id = profiles.id
      and is_business_owner(bookings.business_id)
  ));
  ```

  Narrower in what it exposes — only the business the customer actually booked with sees them, and
  the data stays in `profiles` where its own policy governs it. Wider in what it lets a business
  query. This is the option that keeps a phone number out of a publicly-readable table.

- **C — have the customer's own app copy name and phone into `details` at booking time.** Works
  today with no migration, since a customer may update their own booking. **Not recommended**: it
  writes a phone number into the same publicly-readable `bookings` row as A, without A's benefit of
  being a real column the backend controls, and it puts the copy in the client where it can drift
  from the profile.

B is the recommendation. A is fine if `bookings`' read policy is tightened at the same time.

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
