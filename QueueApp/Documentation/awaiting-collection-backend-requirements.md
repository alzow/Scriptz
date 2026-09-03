# Awaiting Collection — Backend Requirements

Companion to the front-end intent file for the Awaiting Collection flow.

**Status: migrated.** The migration in §2 has been applied to Supabase and the three open
questions below are resolved — see the "Resolved" callouts in §3 and §4. The `TODO: stub`
comments in the C# still stand as a reconciliation checklist (the constants and stubbed fields
need to be swapped/removed against the live schema), but the decisions themselves are settled and
should not be revisited from this file.

The front end was built against a stub of this shape so the UI could be exercised before this
landed. Every stubbed field or endpoint in the C# carries a `// TODO:` comment that points back
here — search the `QueueApp` project for `TODO: stub` to find every call site that needs to be
reconciled with the live schema.

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
and before the final closed state — `done` for `queue_status`, `completed` for `booking_status`;
the two enums don't share a terminal label.

- Front-end stub constants: `QueueEntryStatuses.AwaitingCollection` and
  `BookingStatuses.AwaitingCollection`, both currently the string literal `"awaiting_collection"`.
  If the enum label ends up different, these two constants are the only places to change.
- `queue_entries.status` and `bookings.status` both need the new label added to their respective
  Postgres enums (`ALTER TYPE ... ADD VALUE`).
- The queue engine's `GetActiveEntriesAsync` default filter was widened front-end-side to
  `in.(waiting,serving,awaiting_collection)` (`QueueApp/Services/Api/Queue/IQueueApi.cs`) so the
  operator board keeps awaiting-collection entries in view. No booking-side equivalent was needed —
  the agenda's own queries are unfiltered by status already.

**Known gap, needs a backend fix — `my_active_queue_entry()`.** This RPC backs the browse
dashboard's live queue hero (`IQueueApi.GetMyActiveEntryAsync`, `CategoryPickerPageViewModel.
ActiveEntry`). Its SQL isn't in this repo, but its behavior confirms it filters server-side to some
status set — an entry that transitions to `awaiting_collection` stops coming back at all, and the
hero disappears from the browse dashboard entirely (reported after the migration in this file was
applied; reproduced against the real backend, not the stub — `StubQueueService.GetMyActiveEntryAsync`
had the equivalent bug client-side and has been fixed to include `awaiting_collection`, but that
stub is opt-in via the `USE_STUBS` build flag and isn't what live testing runs against). Whatever
this function's `WHERE status = ...` / `status IN (...)` clause currently reads, it needs
`awaiting_collection` added alongside `waiting` and `serving` — same fix as `GetActiveEntriesAsync`
above, just server-side. `my_queue_status(business_id)` is described as making the same kind of
split and may have the identical gap; lower stakes if so — `VisitPageViewModel.LoadEntryAsync` only
loses the Position/WaitMinutes it supplies, doesn't lose the row. Neither function's definition was
available in this pass to write an exact patch — needs whoever has the live definition (or
Supabase migration history) to apply it.

## 3. Timestamps

- `queue_entries.awaiting_collection_at` (nullable timestamptz) — stamped when an entry enters
  Awaiting Collection.
- `queue_entries.collected_at` (nullable timestamptz) — stamped when the entry is actually
  collected (either side).
- `bookings.awaiting_collection_at` (nullable timestamptz) — same, for bookings.
- `bookings.collected_at` (nullable timestamptz), if bookings don't already have an equivalent
  slot for it.

**Resolved:** `done_at` keeps its existing, schema-wide meaning — "this entry reached its final
closed state" — and is *not* stamped on entering Awaiting Collection, only at Collected. It is
stamped alongside `collected_at` (same timestamp) by both `QueueService.MarkCollectedAsync` and
`StubQueueService.MarkCollectedAsync`. `MarkAwaitingCollectionAsync` on both was already correct
as written — it never touched `done_at`.

**Booking side has no `done_at` column and none was added.** `bookings` never had an equivalent
column, the migration in §2 doesn't add one, and nothing in the booking-side C# reads or writes
one — `MarkBookingCollectedAsync` stamps `status=completed` and `collected_at` only. If the
decisions doc's instruction to also stamp `done_at` on the booking side is revisited, it needs a
column added first; there's nothing to write to today.

Front-end stubs carrying these fields: `QueueEntryResponse`, `MyQueueEntryResponse` (queue),
`AgendaBookingResponse` (booking, `awaiting_collection_at` only — selected via `*` so it degrades
gracefully until the column exists, same trick the rest of that model already uses for
`started_at`). `UpcomingBookingResponse` doesn't carry either timestamp yet; add if the customer-
facing visit page ends up needing to render them.

## 4. Transition writes

**Resolved: PATCH is the shipped implementation, not a stand-in.** The dedicated RPC pair
described below is explicitly deferred — not being built this pass — per the decisions doc. The
known gap (either side can set `status` to any enum value via the existing owner/self-manage PATCH
policy, no state-machine enforcement) is accepted for now. Revisit once real usage on live shops
surfaces it as an actual problem, not before.

| Front-end call | Implementation | Sets |
|---|---|---|
| `IQueueService.MarkAwaitingCollectionAsync` | `PATCH /queue_entries?id=eq.<id>` | `status`, `awaiting_collection_at` |
| `IQueueService.MarkCollectedAsync` | `POST /rpc/complete_entry` then `PATCH /queue_entries?id=eq.<id>` | (RPC's own effect, includes `done_at`), then `collected_at` |
| `IBookingService.MarkBookingAwaitingCollectionAsync` | `PATCH /bookings?id=eq.<id>` | `status` |
| `IBookingService.MarkBookingCollectedAsync` | `PATCH /bookings?id=eq.<id>` | `status=completed`, `collected_at` |

`MarkCollectedAsync` (queue) does **not** PATCH `status` directly — a first pass did, guessing the
terminal `queue_status` label was `"completed"`, and that guess was wrong against the live enum
(`invalid input value for enum queue_status`). **Confirmed: the real label is `"done"`.** This is
now `QueueEntryStatuses.Done` and is the value every local optimistic-update in the operator board
uses (`OperatorQueuePageViewModel.DoneAsync`/`MarkCollectedAsync`, `StubQueueService`) — none of
them PATCH it to the server directly, they only mirror it into local state; the queue engine's
`complete_entry` RPC is what actually writes it. `MarkCollectedAsync` calls that RPC (same one the
non-collection Done path already uses) to close the entry out, then PATCHes only `collected_at`,
which is a plain timestamptz column and carries no enum-label risk. This means `done_at`'s value
comes from whatever `complete_entry` stamps, not from the C# — so it is not necessarily identical
to `collected_at` down to the millisecond, unlike the booking side. `MarkBookingCollectedAsync` was
not affected: `completed` was already a
proven `booking_status` label (returned by `complete_booking`'s own output) before this file
existed, so PATCHing it directly is safe.

This works because both tables already grant the owner (operator) and the customer (self-manage)
UPDATE access to their own rows — the same policy `AssignEntryAsync`, `MoveEntryToEndAsync`,
`MarkBookingNoShowAsync` etc. already rely on. Left deferred, not fixed:

- A raw PATCH lets either side set `status` to anything the enum allows, not just the two
  legal transitions (`serving → awaiting_collection`, `awaiting_collection → completed`). There's
  no state-machine enforcement. (`MarkCollectedAsync` queue-side is the exception — it goes through
  `complete_entry`, which already validates the entry is in a state it can close out.)
- `collected_at` is customer-writable today with no check that the entry was actually in
  `awaiting_collection` first.
- If this ever needs fixing: two RPCs per domain (`mark_awaiting_collection` / `mark_collected`
  for queue, equivalent for bookings), mirroring `complete_entry` / `complete_booking`, validating
  the current status before transitioning and safe for either the operator or the customer to call
  (matching the "either side can close it out, whoever acts first" behavior in the front-end spec
  §4 — no confirmation/matching step assumed there either). Swapping the four service methods
  above from PATCH calls to RPC calls needs no change above the service layer.

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
