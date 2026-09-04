# Historic Snapshot — Backend Requirements

Companion to the front-end intent file for Historic Snapshot (Service, Operator & Pricing). This is a
**requirements list for the backend spec/migration**, not a migration itself — nothing here has been
applied to Supabase.

The front end was built against a stub of this shape so the resolution order could be exercised
before the columns exist. Every stubbed field in the C# carries a `// TODO: stub` comment pointing
back here — search the `QueueApp` project for `TODO: stub` to find everything that needs reconciling
with whatever this spec actually decides.

Three claims in the intent file's §8 don't match the code as it stands. They're corrected in §7
below, and one of them removes work rather than adding it.

---

## 1. Four columns on `queue_entries` and `bookings`

Per the intent file §3 and §4 — columns, not jsonb, and on two tables rather than three.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `service_name` | text | yes | as the service read at the moment the row settled |
| `service_price_cents` | int4 | yes | matches `services.price_cents`, which is itself nullable |
| `service_est_minutes` | int4 | yes | the estimate, not the actual — actuals are already derivable |
| `operator_name` | text | yes | as the operator read at the moment the row settled |

All four nullable, all four with no default. **Null is meaningful**: it says "this row has not
settled yet, read live". That is the whole of the front-end rule (§7), so nothing may backfill a
placeholder into an active row.

`service_id` / `operator_id` stay exactly as they are, including `on delete set null`.

No index is needed. Nothing filters or joins on these — they are read out with the row.

---

## 2. The trigger

One `before update` trigger per table, filling the four columns when `status` transitions **into** a
terminal value.

```sql
create or replace function snapshot_service_and_operator()
returns trigger
language plpgsql
as $$
begin
  select s.name, s.price_cents, s.est_minutes
    into new.service_name, new.service_price_cents, new.service_est_minutes
  from services s
  where s.id = new.service_id;

  select o.display_name
    into new.operator_name
  from operators o
  where o.id = new.operator_id;

  return new;
end;
$$;
```

Bound to each table with a `when` clause so it only fires on the transition, not on every update:

```sql
create trigger queue_entries_snapshot
  before update on queue_entries
  for each row
  when (old.status is distinct from new.status and new.status in (<terminal queue labels>))
  execute function snapshot_service_and_operator();

create trigger bookings_snapshot
  before update on bookings
  for each row
  when (old.status is distinct from new.status and new.status in (<terminal booking labels>))
  execute function snapshot_service_and_operator();
```

### The terminal label sets must be pulled, not assumed

The intent file §10 names `done`, `no_show`, `cancelled` for `queue_entries` and `completed`,
`cancelled`, `no_show` for `bookings`, and says to confirm them. **They cannot be assumed, and this
is the single highest-risk item in this document.** The app itself does not know these labels:

- `QueueEntryStatuses` matches cancellation and no-show *loosely* —
  `status.Contains("cancel")`, `status.Contains("no_show") || status.Contains("noshow")` — with a
  comment saying the `queue_status` labels were never captured.
- `BookingStatuses` says the same of `booking_status`, and lists `in_progress` and `no_show` as
  labels the enum *may not have yet*.
- `SUPABASE-SCHEMA-VERIFIED.md` §1g confirms both are real enums and that the label lists were never
  pulled, and gives the query to pull them.

A `when` clause naming a label the enum doesn't have fails at `create trigger` time, which is the
good outcome. The bad outcome is a label that exists but is spelled differently from the guess — the
trigger then silently never fires for that transition and those rows keep resolving live forever,
with nothing to show that anything is wrong. Run the §1g query first and write the `when` clauses
from its output.

### `awaiting_collection` is not terminal

Both tables use it, and on both it is a waypoint, not an end state: an entry goes
`awaiting_collection` → `done`, a booking `awaiting_collection` → `completed`. The trigger must not
treat it as terminal or the row would be snapshotted and then transition again.

Worth a deliberate decision rather than a silent one: the *work* is finished by the time a row is
awaiting collection, so a price edit made between "ready for collection" and "collected" still moves
that row's figure. The window is short and the recommendation is to leave it — but it is a real hole
in "snapshot at terminal state", and it should be a choice.

### Why this covers every write path

The argument for a trigger over RPC edits is stronger than the intent file states, because **four of
the eight terminal transitions are not RPCs at all** — they are PATCHes issued straight at the table:

| Table | Transition | Path |
|---|---|---|
| `queue_entries` | serving → done | `complete_entry` RPC |
| `queue_entries` | awaiting_collection → done | **PATCH** (`QueueService.MarkCollectedAsync`) |
| `queue_entries` | → cancelled | `cancel_entry` RPC |
| `queue_entries` | → no_show | `mark_no_show` RPC |
| `bookings` | → completed | `complete_booking` RPC |
| `bookings` | awaiting_collection → completed | **PATCH** (`BookingService.MarkBookingCollectedAsync`) |
| `bookings` | → cancelled | `cancel_booking` RPC |
| `bookings` | → no_show | **PATCH** (`BookingService.MarkBookingNoShowAsync`) |

`MarkCollectedAsync` PATCHes rather than calling `complete_entry` deliberately — that RPC's state
machine requires `status = 'serving'` and rejects an entry already in `awaiting_collection`. There is
no RPC covering these three, so an RPC-only approach would leave collected and no-showed rows
permanently unsnapshotted. A trigger is not merely the tidier option here; it is the only one that
covers the whole surface.

### `before insert` is not needed, but is cheap insurance

No current path inserts a row directly in a terminal state — a queue entry starts `waiting`, a
booking `pending` or `confirmed`. Adding the same function as a `before insert` trigger with the same
`when` clause costs nothing and closes the case if a future import or backfill ever does.

### Re-snapshotting on a move (intent file §9.1)

The intent file leans towards re-firing, and the `when` clause above will **not** do it: moving a
completed booking changes `operator_id`, not `status`, so `old.status is distinct from new.status` is
false. If re-fire is wanted, the booking trigger's condition needs a second arm:

```sql
when (
  (old.status is distinct from new.status and new.status in (<terminal booking labels>))
  or (old.status in (<terminal booking labels>) and old.operator_id is distinct from new.operator_id)
)
```

Note what that second arm does to the rest of the snapshot: the function above rewrites all four
columns, so a move would also re-read the service's *current* name and price onto a settled row —
re-introducing exactly the bug this feature exists to fix, on any row anyone ever moves. If re-fire
is wanted, the second arm needs a function that writes `operator_name` **only**. Recommend splitting
it rather than reusing `snapshot_service_and_operator()`.

### Permissions

The intent file's §5 note is correct and confirmed rather than assumed: `services` and `operators`
both carry `public read` with `qual = true` (`SUPABASE-SCHEMA-VERIFIED.md` §3), so a trigger function
running as the invoking user can read both. No `security definer` needed **today**.

One caveat worth writing into the migration: `SUPABASE-SCHEMA-VERIFIED.md` §1b flags that app-wide
`public read` pattern as something to revisit deliberately. If `services` or `operators` reads are
ever tightened, this trigger silently starts writing nulls into the snapshot — no error, no failed
write, just history quietly stopping. Either make the function `security definer` now (it reads two
tables by primary key and writes nothing, so the blast radius is small), or leave it invoker-rights
and add a line to whatever tightens those policies. Making it `security definer` is the safer of the
two and the recommendation here.

---

## 3. `visits` — the FK, and a gap it runs into

Per intent file §6: add `queue_entry_id` and `booking_id` to `visits`, both nullable, both
`on delete set null`, with at most one populated per row.

```sql
alter table visits
  add column queue_entry_id uuid references queue_entries(id) on delete set null,
  add column booking_id     uuid references bookings(id)      on delete set null;
```

A check constraint (`num_nonnulls(queue_entry_id, booking_id) <= 1`) is optional and worth having.

`complete_entry` and `complete_booking` each need one column added to the `insert into visits` they
already run — a single column on an existing insert, no signature change, so none of the overload
risk §5 is avoiding.

### The gap: the collection path never reaches those functions

Both PATCH transitions in the table above bypass the RPC that creates the `visits` row. An entry
collected through `MarkCollectedAsync` goes to `done` without `complete_entry` ever running, and a
booking through `MarkBookingCollectedAsync` reaches `completed` without `complete_booking`.

**So those visits rows are never created at all today** — which is a pre-existing gap, not one this
feature introduces, but it means "populate the FK in `complete_entry`/`complete_booking`" covers less
of the table than it looks like it does. Anything built on the new FK inherits that hole.

This is out of scope here and should not be fixed as a side effect of this migration. It needs
raising on its own, alongside the note already sitting in `IQueueService`
(`awaiting-collection-backend-requirements.md` §4: PATCH-based by design, revisit with a dedicated
RPC if it becomes a problem). The honest summary is that the collection flow trades `visits` rows for
not fighting `complete_entry`'s state machine, and nobody has noticed because nothing reads `visits`
(§7).

---

## 4. Backfill

Terminal rows only. An active row must keep all four columns null so it goes on resolving live.

```sql
update queue_entries e
set service_name        = coalesce(s.name, 'Service'),
    service_price_cents = s.price_cents,
    service_est_minutes = s.est_minutes,
    operator_name       = coalesce(o.display_name, 'Any available')
from (select 1) _
left join services  s on s.id = e.service_id
left join operators o on o.id = e.operator_id
where e.status in (<terminal queue labels>)
  and e.service_name is null;
```

(Written as a sketch — the `left join` shape needs adjusting to a correlated form against `e`; the
point is the `where`, not the join syntax.)

Same for `bookings` with its own terminal labels. Notes:

- **`and service_name is null`** makes it re-runnable and stops it overwriting anything the trigger
  has already written correctly between the two statements.
- **`service_price_cents` gets no fallback.** `services.price_cents` is genuinely nullable — the app
  renders a missing price as "No price set" — so a null there is real data, not an absent snapshot.
  Only the two text columns get a literal.
- The fallbacks `'Service'` and `'Any available'` come from intent file §7. `'Any available'` is what
  the booking side of the app already shows; the queue side shows `'Next available'`, so a queue row
  backfilled with `'Any available'` will read slightly off against neighbouring rows. Using
  `'Next available'` for `queue_entries` and `'Any available'` for `bookings` matches the app's own
  copy — recommended, and a one-word change.
- Per intent file §7, this is approximate for anything renamed before the migration, and there is no
  way to recover the old value. State it in the migration, don't present it as a restoration.
- Do **not** backfill `visits.queue_entry_id` / `booking_id`. Intent file §7 already rules out the
  timestamp-proximity matching that would be needed, and §7 below shows nothing reads `visits`
  anyway.

Finish with the schema cache reload:

```sql
notify pgrst, 'reload schema';
```

Without it PostgREST keeps serving the old column list and every `select=*` comes back without the
new columns — which looks exactly like the trigger not working.

---

## 5. RLS

Nothing to add. The four columns ride along with the existing row-level policies on `queue_entries`
and `bookings`, and must not widen anything. They contain a service name, a price the shop publishes
anyway, and a staff display name — nothing more sensitive than what those rows already carry.

The two `visits` FK columns are likewise covered by the existing `visits` policies (customer reads
own, owner reads their business's).

---

## 6. What the front end already does

Done in the same pass as this document, against the stub. All of it is null-safe against today's
schema and picks the columns up by itself once the migration lands.

**Resolution order, everywhere: snapshot → live embed → literal default.**

The order needs no branch on status, and that is the neat part of terminal-state capture: an active
row has no snapshot, so it falls through to the live embed on its own. One expression serves both
halves of the rule.

| Model | Reads | Change |
|---|---|---|
| `MyQueueEntryResponse` | History list, visit detail | `ServiceName`, `PriceCents`, `OperatorName`, `HasOperator` |
| `UpcomingBookingResponse` | Upcoming + booking history | `ServiceName`, `PriceCents`, `PriceText`, `OperatorName`, `HasOperator` |
| `MyBookingSummaryResponse` | Booking history strip | `ServiceName`, `PriceText`, `OperatorName` |
| `AgendaBookingResponse` | Booking agenda, incl. revenue | `ServiceName`, `PriceCents`, `ServiceMinutes`, `OperatorName` |

Three reads in `IBookingApi` were explicit column lists and are now `select=*` plus their embeds —
`GetMyBookingsAsync`, `GetMyUpcomingBookingsAsync`, `GetMyBookingHistoryAsync`. Naming a column
PostgREST can't find fails the whole query with a 400, so an explicit list would have had to change
in lockstep with the migration; `*` is the same trick `GetAgendaBookingsAsync` and the visit-page
reads already use for `started_at`, and it means these three start returning the snapshot the moment
the migration runs, with no app release.

Two smaller things:

- **`HasOperator` now counts the snapshot.** It was `operator_id is not null`, which is precisely the
  case the intent file §1 calls "actively wrong": deleting an operator nulls the FK, and a settled
  visit that named someone would start reading "Any available". It is now
  `operator_id is not null || operator_name is present`.
- **Fallback literals are in one place** — `Shared/Domain/VisitSnapshotDefaults.cs` — rather than
  repeated across four models as they were.

### Deliberately not changed

- **Operator queue board.** Intent file §8 lists it and concludes live is correct. It is:
  `GetActiveEntriesAsync` filters to `waiting,serving,awaiting_collection`, so no terminal row ever
  reaches the board and a snapshot preference there would be permanently dead code. `QueueEntryResponse`
  is untouched.
- **`service_est_minutes` on the customer-facing models.** The trigger writes it on both tables, but
  nothing customer-facing shows an estimate — the visit page shows actual waited/served durations,
  which §3 correctly notes are already derivable. Only `AgendaBookingResponse` binds it, because
  `ServiceMinutes` already existed there.
- **Anything downstream.** Queue engine, wait calculation, realtime, notifications and the flow are
  all unmodified.

---

## 7. Three corrections to the intent file's §8

Each of these is a claim about the current code that no longer holds. The first removes work.

**1. Nothing in the app reads `visits`.** §8 says "History tab — the completed-visits list. Reads
`visits`; needs the new join from §6." It doesn't. The History tab reads `queue_entries`
(`GetMyEntriesAsync`) and `bookings` (`GetMyBookingHistoryAsync`) directly, and `IQueueService`
carries the note explaining why: *"Replaces the `visits` read: a visits row has a visited_at and
nothing else, so no page built on it can say how long anyone waited."* There is no `/visits` request
anywhere in the project.

So §6's "cost, stated honestly" — the awkward nullable-on-both-sides join — **does not arise**. No
call site grows a join, because no call site reads the table. That does not make the FK worthless:
§6's independent argument holds (there is currently no way to get from a visit back to its source
row), and `visits` still backs the `businesses` "customer read via own visit" RLS policy. But it
should be specified as schema groundwork for later, not as something History needs, and it is
severable from the rest of this migration if the work needs cutting down. The §3 gap above is a
second reason to treat it as its own piece of work.

**2. `GetAgendaBookingsAsync` no longer uses an explicit column list.** §8 says the new fields must
be added to it "or they won't come back at all". It was changed to `select=*` for `started_at`, with
a comment giving this exact reasoning, so it picks the snapshot columns up with no change. The reads
that *did* need changing were the three in §6 above — none of which §8 flags.

**3. The agenda's revenue sum needed no status branch.** §8 asks it to "switch to the snapshotted
price for terminal rows". It sums `PriceCents` over `CountsTowardsRevenue(status)`, which is
everything except cancelled and no-show — so the day's figure legitimately includes pending and
confirmed bookings that haven't happened yet. Making `PriceCents` snapshot-first gives exactly the
wanted behaviour with no branch: settled rows sum at what they settled for, rows still ahead sum at
today's price, which is the figure the shop is actually expecting for the rest of the day.

---

## 8. Open, and not for this migration

- **Intent file §9.1 (re-snapshot on move)** needs the split function described in §2, not the shared
  one. Recommend deciding it before writing the trigger rather than after.
- **The `awaiting_collection` window** in §2 — short, real, currently unhandled.
- **The missing `visits` rows** in §3 — pre-existing, wider than this feature, needs its own raise.
- **Intent file §9.3 (soft-delete services)** is untouched here. Worth noting that this migration
  makes it *less* urgent rather than more: once a settled row carries its own service name, deleting
  a service stops destroying history and becomes merely lossy for active rows.
