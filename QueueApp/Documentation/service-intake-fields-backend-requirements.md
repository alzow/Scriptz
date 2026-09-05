# Service intake fields — backend requirements

> **Status: not applied.** Every route in `IIntakeFieldsApi` and the `requires_collection` column on
> `services` are written against this file, not against anything that exists in Supabase today. The app
> is built to survive their absence — `GetFieldsByServiceAsync` swallows its failure and returns an empty
> map, so a business with no intake fields keeps a working join flow — but nothing here has been created.
>
> Scope note: this covers **storage only**, per §8 of `SERVICEOFFERINGSSPEC.md`. The queue-engine changes
> that collection implies (a status between serving and completed, `ready_at`, `mark_ready`/`mark_collected`,
> the wait-estimate fix) are **not** in this pass and are listed in §5 below so they aren't lost.

---

## 1. `service_intake_fields`

One question a service asks before an entry or booking is created.

| column | type | nullable | default |
|---|---|---|---|
| id | uuid (PK) | no | `gen_random_uuid()` |
| service_id | uuid (FK → `services.id`, cascade) | no | — |
| field_type | text | no | `'short_text'` |
| label | text | no | — |
| **hint** | text | yes | — |
| is_required | bool | no | `false` |
| sort_order | int4 | no | `0` |
| options | jsonb | yes | — |
| visibility_rule | jsonb | yes | — |
| created_at | timestamptz | no | `now()` |

**`field_type`** is one of `short_text`, `long_text`, `single_select`, `multi_select`, `file`. Kept as text
with a check constraint rather than an enum so adding a kind is a migration, not an enum alter under load.

**`options`** is a JSON array of strings, meaningful only for the two select kinds. A select needs at least
two — the editor enforces this; a check constraint would be reasonable but is not required.

**`visibility_rule`** is `{"field_id": "<uuid>", "values": ["Medical aid"]}`, or null when the question is
always asked. One jsonb object rather than two columns so there is no partial state to null out, and so a
later `not_in` or multi-condition shape lands without a migration.

**The ordering rule the app enforces and the database does not:** a `visibility_rule` may only reference a
question in the same service with a *lower* `sort_order`. The editor refuses a reorder that would break this
and names both questions. A trigger asserting it would be a belt-and-braces improvement.

### Suggested DDL

```sql
create table public.service_intake_fields (
    id              uuid primary key default gen_random_uuid(),
    service_id      uuid not null references public.services(id) on delete cascade,
    field_type      text not null default 'short_text'
                    check (field_type in ('short_text','long_text','single_select','multi_select','file')),
    label           text not null,
    hint            text,
    is_required     boolean not null default false,
    sort_order      integer not null default 0,
    options         jsonb,
    visibility_rule jsonb,
    created_at      timestamptz not null default now()
);

create index service_intake_fields_service_idx
    on public.service_intake_fields (service_id, sort_order);
```

### RLS

The customer flow reads these before joining, so read has to be public in the same way `services` already is.
Writes are the owning business only.

| policy | command | condition |
|---|---|---|
| public read | SELECT | `true` |
| owner insert/update/delete | ALL | `is_business_owner((select business_id from services where services.id = service_id))` |

```sql
alter table public.service_intake_fields enable row level security;

create policy "public read" on public.service_intake_fields
    for select using (true);

create policy "owner writes" on public.service_intake_fields
    for all using (
        exists (
            select 1 from public.services s
            where s.id = service_intake_fields.service_id
              and public.is_business_owner(s.business_id)
        )
    );
```

`GetFieldsForBusinessAsync` reads through `services!inner(business_id)`, so the embedded select needs the
existing public read on `services` — which it has.

---

## 2. `services.requires_collection`

```sql
alter table public.services
    add column requires_collection boolean not null default false;
```

**Naming:** the spec calls this `has_collection_step`. The column shipped in `ServiceResponse` as
`requires_collection` before that spec landed, and the Refit contract, the queue board's
`awaiting_collection` status label and `brd20_StatusPill_Selectable`'s trigger all already use the
"collection" wording. Renaming to match the spec buys nothing and breaks a contract that is already in the
app, so `requires_collection` stands. Per §12 of the spec the operator-facing word stays **Ready** and the
data word stays *collection* — they do not have to match.

Nothing consumes this column yet. It is stored and displayed only; the engine work in §5 is what makes it do
anything.

---

## 3. Answers

Not needed for this pass — nothing writes answers until the intake step commits them. Recorded so the shape
isn't re-litigated later: **one** table keyed to a queue entry *or* a booking, with two nullable foreign keys,
rather than two near-identical tables. Answers are already stored self-describing (see `IntakeAnswer`), so
editing or deleting a question never rewrites history.

---

## 4. Files — the POPIA decision

`IntakeFileService` uploads to the bucket named by `SupabaseConfig.IntakeUploadsBucket` under the key
`{userId}/{serviceId}/{fieldId}/{guid}{ext}`, and downloads through
`storage/v1/object/authenticated/...`. That code ships today.

**Decision taken this pass: the `File` chip ships, with the warning.** The editor shows *"Files are personal
information — stored with the visit, visible to anyone who can open your board. Ask for documents only when
you truly need them."* whenever `File` is the selected kind.

**Still outstanding, and required before these settings reach a real business:**

- **Bucket RLS.** Read must be scoped so only the owning business and the customer who uploaded it can fetch
  an object. The key's first segment is the uploader's user id, which gives a policy something to match on,
  but the *business* side of that policy needs the visit to resolve business ownership.
- **Size and type limits**, enforced at the bucket, not only by the picker. The picker allows images and PDF;
  nothing currently caps size.
- **A retention rule.** A registration number is harmless; a medical aid card is special personal information
  under POPIA, with different lawful grounds and different breach consequences. Probably a per-kind rule
  rather than one blanket age.

> A clinic asking for a medical aid card is collecting special personal information. Deciding retention after
> a shop has 400 card photos in a bucket is deciding it too late.

---

## 5. Not this pass — the queue engine

Listed so it isn't lost. None of it is built, and `requires_collection` does nothing until it is.

- A status between serving and completed, plus a `ready_at` timestamp. `QueueEntryStatuses.AwaitingCollection`
  already carries the label the app would send.
- **An entry awaiting collection must not occupy a bay or operator.** Four finished cars in the yard must not
  make the wait estimate report the shop full. This is the single most important line in the spec — get it
  wrong and a busy car wash reports impossible waits and stops taking joins.
- `operator_avg_minutes` must measure start → ready, not start → collected, or one customer who fetches their
  car the next morning poisons every estimate the shop shows.
- `complete_entry` splits into `mark_ready` and `mark_collected` for services with the step; services without
  it keep the single call. The board's `Done` becomes `Mark ready` **per entry**, not per business — a shop
  can have both kinds in one queue.
- `bookings.customer_note` / the queue's free-text note now does the same job as a long-text question. Fold it
  in as a default optional long-text question the business can remove, rather than keeping two paths to the
  same box and two places the operator has to look.
