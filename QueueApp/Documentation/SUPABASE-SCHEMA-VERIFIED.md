# Queue — Verified Schema (Ground Truth)

> Generated from actual `information_schema`/`pg_catalog` output run against the live Supabase project —
> not reconstructed from memory or chat history. Supersedes the schema sections of `SUPABASE-ARCHITECTURE.md`
> and corrects two assumptions in `ROADMAP-next-session.md`. Read §1 first — it's the part that changes what
> you do next, not just what's documented.

---

## 1. Corrections and findings — read this part first

**1a. The booking-table RLS gap flagged in the roadmap doesn't exist. It's already fixed.**

`operator_availability`, `availability_blocks`, and `bookings` all have real, working policies — not the
deny-all state assumed last session:

- `operator_availability` / `availability_blocks`: public read, owner insert/update/delete via `owns_operator`.
- `bookings`: public read; insert allowed for `auth.uid() = customer_id` **or** `is_business_owner`; owner
  delete; owner-or-self update.

**This means the roadmap's "fix RLS first" step for the booking backend is done.** The actual next step there
is slot-generation logic, not RLS — skip straight to that when picking the booking work back up.

**1b. `v_queue_positions` is a real, currently-exposed privacy gap — but not the one originally suspected.**

Back in Step 6, `my_queue_status` was built as a `security definer` function specifically to avoid querying
`v_queue_positions` directly, on the theory that a plain view might bypass the underlying table's RLS. Turns
out that theory doesn't even need to apply here, because **`queue_entries` itself has a `public read`
policy with `qual = true`** — meaning any signed-in user can already run
`GET /rest/v1/queue_entries?select=*` directly and see every business's full queue, including every waiting
customer's `customer_name` and `customer_id`, not just their own.

`v_queue_positions` (visible in your Supabase dashboard flagged **"UNRESTRICTED"**) just re-exposes that same
already-public data with a position number attached. It also has a narrower bug on top: its `where` clause is
`status = 'waiting'` only, so it silently omits anyone currently `'serving'`.

**This app-wide "public read" pattern is itself worth revisiting**, not just this one view. It's applied to
`businesses` (active only), `operators`, `services`, `queue_entries`, and `bookings`. It made sense under the
original "browse without signing in" design — but that was walked back at 4d; login is now required before
reaching any screen. With that correction in place, "public" effectively means "any signed-in stranger," which
is broader than customer-name data probably needs to be. Worth a deliberate decision, not a silent fix:

- **Option A — tighten `queue_entries`'s read policy** so a customer's name/id is only visible to the business
  that owns the entry or the customer themselves, and let `business_queue_summary`/`my_queue_status` (both
  already `security definer`) remain the sanctioned way to get aggregate/position data without exposing raw
  rows.
- **Option B — leave it,** if broad visibility inside a small, trusted-by-nature local app is an acceptable
  trade-off you're making deliberately.

Either way, `v_queue_positions` itself should probably be dropped — `my_queue_status` already replaced its
purpose, and it's dead weight sitting flagged "UNRESTRICTED" for no active reason.

**1c. `businesses` already has `latitude`/`longitude` columns.** The architecture doc's "what's not built yet"
section claimed no geo columns existed — that was wrong. They're there, dormant, just as unused as the
booking tables. Doesn't change the earlier "don't build real GPS search yet" call, just corrects the reason —
it's not a schema gap anymore, only a missing UI/query gap.

**1d. `operators.profile_id` exists and was never previously documented.** Nullable, `on delete set null`,
references `profiles(id)`. This means an operator row *can* be linked to a real signed-in account — worth
understanding the intent here before the staff-management step, since it changes whether "add an operator"
means "create a display name" or "link an existing/invite a real user."

**1e. `handle_new_user` likely explains the "Customer" fallback bug's root cause.** This trigger fires on new
`auth.users` rows and inserts a `profiles` row using `new.raw_user_meta_data->>'display_name'`. If your actual
sign-up call never sends `display_name` in the GoTrue sign-up metadata, this trigger inserts `null` every
time — consistent with what was observed. Worth checking whether sign-up should be passing that metadata
rather than relying solely on the Profile tab to backfill it later.

**1f. `rls_auto_enable` is a schema-wide event trigger** that automatically runs `enable row level security`
on any new table created in `public`. Explains why RLS was already on for every dormant table without an
explicit per-table statement — worth knowing this exists so a future table's *policies* still need writing
even though RLS itself will already be on by the time you create it.

**1g. Enum value sets weren't captured by this query round.** `business_category`, `business_mode`,
`business_plan`, `queue_status`, and `booking_status` are all confirmed as real Postgres enums (not free
text), but their actual label lists weren't pulled. If needed:
```sql
select t.typname, e.enumlabel
from pg_type t join pg_enum e on t.oid = e.enumtypid
where t.typname in ('business_category','business_mode','business_plan','queue_status','booking_status')
order by t.typname, e.enumsortorder;
```

---

## 2. Tables (verified columns)

### `profiles`
| column | type | nullable | default |
|---|---|---|---|
| id | uuid (PK, FK → `auth.users.id`, cascade) | no | — |
| phone | text (unique) | yes | — |
| phone_verified | bool | no | `false` |
| display_name | text | yes | — |
| created_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

### `businesses`
| column | type | nullable | default |
|---|---|---|---|
| id | uuid (PK) | no | `gen_random_uuid()` |
| owner_id | uuid (FK → `profiles.id`, cascade) | no | — |
| category | `business_category` enum | no | `'barber'` |
| mode | `business_mode` enum | no | `'queue'` |
| name | text | no | — |
| suburb | text | no | `'Lenasia'` |
| address | text | yes | — |
| **latitude** | float8 | yes | — |
| **longitude** | float8 | yes | — |
| phone | text | yes | — |
| is_active | bool | no | `true` |
| last_seen_at | timestamptz | yes | — |
| plan | `business_plan` enum | no | `'free_trial'` |
| created_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

### `operators`
| column | type | nullable | default |
|---|---|---|---|
| id | uuid (PK) | no | `gen_random_uuid()` |
| business_id | uuid (FK → `businesses.id`, cascade) | no | — |
| **profile_id** | uuid (FK → `profiles.id`, set null) | yes | — |
| display_name | text | no | — |
| is_available | bool | no | `true` |
| sort_order | int4 | no | `0` |
| created_at | timestamptz | no | `now()` |

### `services`
| column | type | nullable | default |
|---|---|---|---|
| id | uuid (PK) | no | `gen_random_uuid()` |
| business_id | uuid (FK → `businesses.id`, cascade) | no | — |
| name | text | no | — |
| **price_cents** | int4 | yes | — |
| est_minutes | int4 | no | `15` |
| is_active | bool | no | `true` |
| sort_order | int4 | no | `0` |
| created_at | timestamptz | no | `now()` |

### `queue_entries`
| column | type | nullable | default |
|---|---|---|---|
| id | uuid (PK) | no | `gen_random_uuid()` |
| business_id | uuid (FK → `businesses.id`, cascade) | no | — |
| operator_id | uuid (FK → `operators.id`, set null) | yes | — |
| service_id | uuid (FK → `services.id`, set null) | yes | — |
| customer_id | uuid (FK → `profiles.id`, set null) | yes | — |
| customer_name | text | yes | — |
| status | `queue_status` enum | no | `'waiting'` |
| joined_at | timestamptz | no | `now()` |
| serving_at | timestamptz | yes | — |
| done_at | timestamptz | yes | — |
| note | text | yes | — |
| details | jsonb | yes | — |
| created_at | timestamptz | no | `now()` |

### `visits`
| column | type | nullable | default |
|---|---|---|---|
| id | uuid (PK) | no | `gen_random_uuid()` |
| business_id | uuid (FK → `businesses.id`, cascade) | no | — |
| operator_id | uuid (FK → `operators.id`, set null) | yes | — |
| customer_id | uuid (FK → `profiles.id`, set null) | yes | — |
| service_id | uuid (FK → `services.id`, set null) | yes | — |
| visited_at | timestamptz | no | `now()` |
| created_at | timestamptz | no | `now()` |

### `operator_availability` *(dormant — booking mode)*
| column | type | nullable | default |
|---|---|---|---|
| id | uuid (PK) | no | `gen_random_uuid()` |
| operator_id | uuid (FK → `operators.id`, cascade) | no | — |
| day_of_week | int2, `check 0–6` | no | — |
| start_time | time | no | — |
| end_time | time, `check end_time > start_time` | no | — |
| created_at | timestamptz | no | `now()` |

### `availability_blocks` *(dormant — booking mode)*
| column | type | nullable | default |
|---|---|---|---|
| id | uuid (PK) | no | `gen_random_uuid()` |
| operator_id | uuid (FK → `operators.id`, cascade) | no | — |
| starts_at | timestamptz | no | — |
| ends_at | timestamptz, `check ends_at > starts_at` | no | — |
| reason | text | yes | — |
| created_at | timestamptz | no | `now()` |

### `bookings` *(dormant — booking mode)*
| column | type | nullable | default |
|---|---|---|---|
| id | uuid (PK) | no | `gen_random_uuid()` |
| business_id | uuid (FK → `businesses.id`, cascade) | no | — |
| operator_id | uuid (FK → `operators.id`, cascade) | no | — |
| service_id | uuid (FK → `services.id`, set null) | yes | — |
| customer_id | uuid (FK → `profiles.id`, set null) | yes | — |
| starts_at | timestamptz | no | — |
| ends_at | timestamptz, `check ends_at > starts_at` | no | — |
| status | `booking_status` enum | no | `'pending'` |
| note | text | yes | — |
| **details** | jsonb | yes | — |
| created_at | timestamptz | no | `now()` |

Plus the double-booking exclusion constraint (unchanged from Step 1):
```sql
constraint bookings_no_overlap
  exclude using gist (
    operator_id with =,
    tstzrange(starts_at, ends_at) with &&
  ) where (status in ('pending','confirmed'))
```

---

## 3. RLS policies (full, verified)

| table | policy | command | condition |
|---|---|---|---|
| `businesses` | public read active | SELECT | `is_active = true` |
| `businesses` | owner read own | SELECT | `auth.uid() = owner_id` |
| `businesses` | customer read via own visit | SELECT | `exists (select 1 from visits where visits.business_id = businesses.id and visits.customer_id = auth.uid())` |
| `businesses` | owner insert | INSERT | `auth.uid() = owner_id` |
| `businesses` | owner update | UPDATE | `auth.uid() = owner_id` |
| `businesses` | owner delete | DELETE | `auth.uid() = owner_id` |
| `operators` | public read | SELECT | `true` |
| `operators` | owner insert/update/delete | — | `is_business_owner(business_id)` |
| `services` | public read | SELECT | `true` |
| `services` | owner insert/update/delete | — | `is_business_owner(business_id)` |
| `queue_entries` | public read | SELECT | `true` — see §1b |
| `queue_entries` | customer self-join or owner add | INSERT | `auth.uid() = customer_id OR is_business_owner(business_id)` |
| `queue_entries` | owner or self manage | UPDATE | `is_business_owner(business_id) OR auth.uid() = customer_id` |
| `queue_entries` | owner delete | DELETE | `is_business_owner(business_id)` |
| `visits` | customer read own | SELECT | `auth.uid() = customer_id` |
| `visits` | owner read | SELECT | `is_business_owner(business_id)` |
| `visits` | owner insert | INSERT | `is_business_owner(business_id)` |
| `operator_availability` | public read | SELECT | `true` |
| `operator_availability` | owner insert/update/delete | — | `owns_operator(operator_id)` |
| `availability_blocks` | public read | SELECT | `true` |
| `availability_blocks` | owner insert/update/delete | — | `owns_operator(operator_id)` |
| `bookings` | public read | SELECT | `true` — see §1b |
| `bookings` | customer self-book or owner add | INSERT | `auth.uid() = customer_id OR is_business_owner(business_id)` |
| `bookings` | owner or self manage | UPDATE | `is_business_owner(business_id) OR auth.uid() = customer_id` |
| `bookings` | owner delete | DELETE | `is_business_owner(business_id)` |
| `profiles` | self read | SELECT | `auth.uid() = id` |
| `profiles` | self update | UPDATE | `auth.uid() = id` |

---

## 4. Functions (app-relevant only — `btree_gist` internals omitted)

All verified against live source, matching what's documented in the step files with no drift found:

`is_business_owner`, `owns_operator`, `join_queue`, `start_serving`, `complete_entry`, `cancel_entry`,
`mark_no_show`, `operator_avg_minutes`, `queue_entry_wait_minutes`, `business_queue_summary`,
`my_queue_status` — all confirmed matching current step files, including the `count(*) >= 3` guard and
`greatest(avg, 1)` floor on `operator_avg_minutes`, and the ranking-before-filtering fix on `my_queue_status`.

**New, not previously documented:**
- **`handle_new_user()`** — trigger function, inserts a `profiles` row on new `auth.users` signup, pulling
  `display_name` from `raw_user_meta_data`. See §1e.
- **`rls_auto_enable()`** — event trigger, auto-enables RLS on any new `public` table. See §1f.

---

## 5. Views

### `v_queue_positions` — flagged for removal, see §1b
```sql
select id, business_id, operator_id, service_id, customer_id, customer_name, status, joined_at,
       row_number() over (partition by business_id, operator_id order by joined_at) as "position"
from queue_entries
where status = 'waiting';
```
No RLS of its own (Supabase dashboard shows it as **UNRESTRICTED**); omits `'serving'` entries; superseded by
`my_queue_status`. Candidate for `drop view v_queue_positions;` unless something still depends on it.

---

## 6. Extensions

`btree_gist` — powers the `bookings_no_overlap` exclusion constraint (§2). No other app-level dependency on it.
