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

**Found, confirmed, still not applied as of the last check — `my_active_queue_entry()`.** This RPC
backs the browse dashboard's live queue hero (`IQueueApi.GetMyActiveEntryAsync`,
`CategoryPickerPageViewModel.ActiveEntry`). Its `mine` CTE filters `status in ('waiting',
'serving')`, so an entry that transitions to `awaiting_collection` stops coming back at all and the
hero disappears from the browse dashboard entirely. Confirmed directly against the live definition
(`pg_get_functiondef`) — full header included below so this is a drop-in replacement, not a partial
patch:

```sql
CREATE OR REPLACE FUNCTION public.my_active_queue_entry()
 RETURNS TABLE(entry_id uuid, business_id uuid, business_name text, business_latitude double precision, business_longitude double precision, operator_id uuid, operator_name text, queue_position integer, status text, joined_at timestamp with time zone, wait_minutes numeric, progress_status text)
 LANGUAGE sql
 STABLE SECURITY DEFINER
 SET search_path TO 'public'
AS $function$
  with mine as (
    select qe.*
    from queue_entries qe
    where qe.customer_id = auth.uid()
      and qe.status in ('waiting', 'serving', 'awaiting_collection')
    order by
      case qe.status
        when 'serving' then 0
        when 'awaiting_collection' then 1
        else 2
      end,
      qe.joined_at asc
    limit 1
  ),
  ahead as (
    -- Unchanged on purpose: an awaiting-collection entry isn't blocking anyone in line, so it
    -- stays out of the "how many are ahead of me" count.
    select count(*)::int as n
    from queue_entries qe
    cross join mine m
    where qe.business_id = m.business_id
      and qe.status in ('waiting', 'serving')
      and qe.joined_at < m.joined_at
      and (
        m.operator_id  is null
        or qe.operator_id is null
        or qe.operator_id = m.operator_id
      )
  )
  select
    m.id,
    m.business_id,
    b.name,
    b.latitude,
    b.longitude,
    m.operator_id,
    o.display_name,
    case when m.status in ('serving', 'awaiting_collection') then 1
         else (select n from ahead) + 1 end,
    m.status::text,
    m.joined_at,
    queue_entry_wait_minutes(m.id),
    m.progress_status
  from mine m
  join businesses b on b.id = m.business_id
  left join operators o on o.id = m.operator_id;
$function$;
```

Three changes from the original: (1) `mine`'s status filter includes `awaiting_collection`; (2) its
`order by` became a 3-way case (serving, then awaiting_collection, then waiting) instead of a
2-way one, in case a customer somehow has more than one live-ish row at once; (3) the
`queue_position` case returns 1 for `awaiting_collection` too, matching `serving` — the front end
never reads this value once it's in that state (`ShowWaitEstimate`/`ShowUnassignedNotice` are both
gated off), so this is cosmetic, just avoiding a nonsense number over a meaningless one.
`queue_entry_wait_minutes(m.id)` wasn't inspected — worth a quick check that it doesn't assume
`status='waiting'` internally, though nothing in the front end depends on its output for an
awaiting-collection row either. RLS was checked and ruled out as a contributing factor — `queue:
public read` has no status condition (`qual = true`), so this function's own filter is the only
blocker.

`my_queue_status(business_id)` is described elsewhere in this codebase as making the same kind of
split and may have the identical gap; lower stakes if so — `VisitPageViewModel.LoadEntryAsync` only
loses the Position/WaitMinutes it supplies, doesn't lose the row. Its definition wasn't available in
this pass to check or patch.

**Second bug found while fixing the first — `complete_entry` and `MarkCollectedAsync`.**
`IQueueService.MarkCollectedAsync` briefly went through `complete_entry` (see §4) to avoid guessing
the terminal status label. That RPC enforces `status='serving'` and rejects anything else with
`entry is not completable` — by the time `MarkCollectedAsync` runs the entry is in
`awaiting_collection`, not `serving`, so it always failed. Now that the terminal label is confirmed
(`done`, not a guess), `MarkCollectedAsync` PATCHes `status`, `done_at` and `collected_at` directly
instead — see §4 for the full history.

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
| `IQueueService.MarkCollectedAsync` | `PATCH /queue_entries?id=eq.<id>` | `status=done`, `done_at`, `collected_at` |
| `IBookingService.MarkBookingAwaitingCollectionAsync` | `PATCH /bookings?id=eq.<id>` | `status` |
| `IBookingService.MarkBookingCollectedAsync` | `PATCH /bookings?id=eq.<id>` | `status=completed`, `collected_at` |

`MarkCollectedAsync` (queue) went through two wrong shapes before landing here, both worth
recording so nobody re-derives them:

1. First pass PATCHed `status="completed"` directly — a guess. `QueueEntryStatuses.cs` already
   flagged the terminal `queue_status` label as never independently confirmed (only `waiting` and
   `serving` are labels this app itself sends). The guess was wrong: `invalid input value for enum
   queue_status`.
2. Second pass avoided guessing by calling `complete_entry` (the same RPC the non-collection Done
   path uses) instead of PATCHing `status` at all. That RPC has its own state-machine check —
   it requires `status='serving'` and rejects anything else with `entry is not completable` — and
   by the time `MarkCollectedAsync` runs, the entry is in `awaiting_collection`, not `serving`. It
   was never going to work for this transition.

**Confirmed: the real terminal label is `"done"`** (`QueueEntryStatuses.Done`), so the shipped
version PATCHes `status`, `done_at` and `collected_at` directly — no RPC, since none exists for
this transition and `complete_entry` doesn't apply outside its own `serving → done` case. Every
local optimistic-update on the queue side (`OperatorQueuePageViewModel.DoneAsync`/
`MarkCollectedAsync`, `StubQueueService`) uses the same confirmed constant.
`MarkBookingCollectedAsync` never had this problem: `completed` was already a proven
`booking_status` label (returned by `complete_booking`'s own output) before this file existed, so
PATCHing it directly was always safe.

This works because both tables already grant the owner (operator) and the customer (self-manage)
UPDATE access to their own rows — the same policy `AssignEntryAsync`, `MoveEntryToEndAsync`,
`MarkBookingNoShowAsync` etc. already rely on. Left deferred, not fixed:

- A raw PATCH lets either side set `status` to anything the enum allows, not just the legal
  transitions (`serving → awaiting_collection → done`, or `serving → done` directly). There's no
  state-machine enforcement — unlike `complete_entry`/`complete_booking`, which is exactly why
  `MarkCollectedAsync` can't reuse them for this new transition.
- `done_at` and `collected_at` are customer-writable today with no check that the entry was
  actually in `awaiting_collection` first.
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
