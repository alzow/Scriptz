# Awaiting Collection — Backend Requirements

Companion to the front-end intent file for the Awaiting Collection flow. This is a **requirements
list for the backend spec/migration**, not a migration itself — nothing here has been applied to
Supabase. Per that file's §10, no schema, enum, column, or RLS change was made in the front-end
pass; this document exists so the backend work has a single, explicit target to build against.

The front end was built against a stub of this shape so the UI could be exercised before this
lands. Every stubbed field or endpoint in the C# carries a `// TODO:` comment that points back
here — search the `QueueApp` project for `TODO: stub` to find every call site that needs to be
reconciled with whatever this spec actually decides.

---

## 1. `requires_collection` flag on `services`

A new boolean column on `services`, defaulting to `false`. Marks a service as using the extended
flow (finish ≠ done — the customer has to come collect the result).

- Front-end stub: `ServiceResponse.RequiresCollection`
  (`QueueApp/Services/Api/ServiceOfferings/Models/ServiceResponse.cs`), read via `services?select=...`.
- Needs a way for the business owner to set it per service — out of scope for this document (a
  Settings UI concern), but the column has to exist and be selectable before that UI can be built.
- Naming is this spec's call; the front end reads it under `requires_collection` purely for
  readability. Rename freely, just update the JSON property name in `ServiceResponse`.

## 2. `awaiting_collection` status value

Both `queue_status` and `booking_status` need a new enum label sitting after "serving" / the slot
and before the final closed state (`completed`, in both cases, on the current schema).

- Front-end stub constants: `QueueEntryStatuses.AwaitingCollection` and
  `BookingStatuses.AwaitingCollection`, both currently the string literal `"awaiting_collection"`.
  If the enum label ends up different, these two constants are the only places to change.
- `queue_entries.status` and `bookings.status` both need the new label added to their respective
  Postgres enums (`ALTER TYPE ... ADD VALUE`).
- The queue engine's `GetActiveEntriesAsync` default filter was widened front-end-side to
  `in.(waiting,serving,awaiting_collection)` (`QueueApp/Services/Api/Queue/IQueueApi.cs`) so the
  operator board keeps awaiting-collection entries in view. No booking-side equivalent was needed —
  the agenda's own queries are unfiltered by status already.

## 3. Timestamps

- `queue_entries.awaiting_collection_at` (nullable timestamptz) — stamped when an entry enters
  Awaiting Collection.
- `queue_entries.collected_at` (nullable timestamptz) — stamped when the entry is actually
  collected (either side).
- `bookings.awaiting_collection_at` (nullable timestamptz) — same, for bookings.
- `bookings.collected_at` (nullable timestamptz), if bookings don't already have an equivalent
  slot for it.

**Open question this spec needs to settle:** whether `done_at` (queue) continues to be stamped when
an entry enters Awaiting Collection, or only once it's actually collected. The front end does not
assume either way — `VisitRecord.ResolveEntryLifecycle` checks `status == awaiting_collection`
ahead of `done_at`, specifically so it doesn't have to guess. Whichever this spec picks, no
front-end change should be needed as long as `status` is authoritative.

Front-end stubs carrying these fields: `QueueEntryResponse`, `MyQueueEntryResponse` (queue),
`AgendaBookingResponse` (booking, `awaiting_collection_at` only — selected via `*` so it degrades
gracefully until the column exists, same trick the rest of that model already uses for
`started_at`). `UpcomingBookingResponse` doesn't carry either timestamp yet; add if the customer-
facing visit page ends up needing to render them.

## 4. Transition writes

No dedicated RPC exists for either transition yet. The front end goes through the existing direct
owner-update PATCH policy on `queue_entries` / `bookings` ("owner or self manage") as a stand-in:

| Front-end call | Today's implementation | Sets |
|---|---|---|
| `IQueueService.MarkAwaitingCollectionAsync` | `PATCH /queue_entries?id=eq.<id>` | `status`, `awaiting_collection_at` |
| `IQueueService.MarkCollectedAsync` | `PATCH /queue_entries?id=eq.<id>` | `status=completed`, `collected_at` |
| `IBookingService.MarkBookingAwaitingCollectionAsync` | `PATCH /bookings?id=eq.<id>` | `status` |
| `IBookingService.MarkBookingCollectedAsync` | `PATCH /bookings?id=eq.<id>` | `status=completed` |

This works today because both tables already grant the owner (operator) and the customer
(self-manage) UPDATE access to their own rows — the same policy `AssignEntryAsync`,
`MoveEntryToEndAsync`, `MarkBookingNoShowAsync` etc. already rely on. It is almost certainly **not**
what should ship long-term:

- A raw PATCH lets either side set `status` to anything the enum allows, not just the two
  legal transitions (`serving → awaiting_collection`, `awaiting_collection → completed`). There's
  no state-machine enforcement.
- `collected_at` is customer-writable today with no check that the entry was actually in
  `awaiting_collection` first.
- Recommend two RPCs per domain (`mark_awaiting_collection` / `mark_collected` for queue,
  equivalent for bookings), mirroring `complete_entry` / `complete_booking`, that validate the
  current status before transitioning and are safe for either the operator or the customer to call
  (matching the "either side can close it out, whoever acts first" behavior in the front-end spec
  §4 — no confirmation/matching step assumed there either).
- Once those RPCs exist, swap the four service methods above from PATCH calls to RPC calls; no
  front-end caller above the service layer needs to change.

## 5. RLS

No new policies were needed for the PATCH-based stand-in above — it rides the existing "owner or
self manage" UPDATE policies on `queue_entries` and `bookings`. If this spec moves to dedicated
RPCs (recommended, see §4), those RPCs should run as `SECURITY DEFINER` the same way
`complete_entry` / `complete_booking` do, checking that the caller is either the entry/booking's
customer or the business owner before transitioning.

## 6. Push notification trigger

The front end added a customer-facing opt-out toggle ("Ready for collection", under a new WHEN
IT'S READY section — `QueueApp/Features/Profile/ProfileNotificationsPage.xaml`,
`NotificationPreferences.AwaitingCollectionReady`) matching the existing per-category toggles
(Time to leave, You're next, etc.). That's the full front-end surface for this — there is no
client-side push-sending code in this repo to extend; the existing push architecture referenced by
the front-end spec (§9) is server-side.

What's needed backend-side: whatever mechanism sends the other push categories today (a DB trigger
or edge function watching for status transitions, presumably) needs a new trigger event firing when
a `queue_entries` or `bookings` row transitions into `awaiting_collection`, gated on
`AwaitingCollectionReady` being true for the recipient's stored preferences. No new
infrastructure — same chain, new trigger condition — per the front-end spec's explicit note that
this is out of scope for the front-end pass beyond flagging it.

## 7. Summary of every stub marker in the C#

Search `QueueApp/` for `TODO: stub` to find all of these:

- `Services/Api/Queue/Models/QueueEntryStatuses.cs` — `AwaitingCollection` constant
- `Services/Api/Booking/Models/BookingStatuses.cs` — `AwaitingCollection` constant
- `Services/Api/ServiceOfferings/Models/ServiceResponse.cs` — `RequiresCollection` flag
- `Services/Api/Queue/Models/QueueEntryResponse.cs` — `AwaitingCollectionAt`, `CollectedAt`
- `Services/Api/Queue/Models/MyQueueEntryResponse.cs` — `AwaitingCollectionAt`, `CollectedAt`
- `Services/Api/Booking/Models/AgendaBookingResponse.cs` — `AwaitingCollectionAt`
- `Services/Api/Queue/IQueueService.cs` / `QueueService.cs` — `MarkAwaitingCollectionAsync`,
  `MarkCollectedAsync`
- `Services/Api/Booking/IBookingService.cs` / `BookingService.cs` — `MarkBookingAwaitingCollectionAsync`,
  `MarkBookingCollectedAsync`
