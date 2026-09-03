# Service Intake Fields — Backend Requirements

Companion to the front-end intent file for Service Intake Fields. This is a **requirements list for
the backend spec/migration**, not a migration itself — nothing here has been applied to Supabase.
Per that file's §8, no schema, table, column, bucket or RLS change was made in the front-end pass;
this document exists so the backend work has a single, explicit target to build against.

The front end was built against a stub of this shape so the UI could be exercised before this
lands. Every stubbed field, route or call site in the C# carries a `// TODO: stub` comment that
points back here — search the `QueueApp` project for `TODO: stub` to find everything that needs to
be reconciled with whatever this spec actually decides.

---

## 1. `service_intake_fields` table

One row per question a service asks. New table.

| Column | Type | Notes |
|---|---|---|
| `id` | uuid, PK | |
| `service_id` | uuid, FK → `services(id)` | cascade delete with the service |
| `field_type` | text or enum | one of `short_text`, `long_text`, `file`, `single_select`, `multi_select` |
| `label` | text, not null | the question as the customer sees it |
| `is_required` | boolean, not null, default false | blocks advancing past the intake step |
| `sort_order` | int, not null, default 0 | display order within the service |
| `options` | text[] or jsonb array | only meaningful for the two select types; null otherwise |

- Front-end stub: `IntakeFieldResponse`
  (`QueueApp/Services/Api/Intake/Models/IntakeFieldResponse.cs`), read via `select=*`.
- The five type values live in one place front-end-side —
  `QueueApp/Services/Api/Intake/Models/IntakeFieldTypes.cs`. If the labels end up spelled
  differently, those five constants are the only edit.
- `options` is read as a JSON array of strings. A Postgres `text[]` serialises to PostgREST as a
  JSON array, so either representation works without a front-end change.

**Reads the front end makes:**

1. Per business, for the customer flow — every field for every service the business offers:
   ```
   GET /service_intake_fields?select=*,service:services!inner(business_id)
       &service.business_id=eq.<id>&order=sort_order.asc
   ```
   This is the one read on the join path, and the one round-trip this feature adds to it. It fails
   soft front-end-side (`IntakeFieldsService.GetFieldsByServiceAsync` catches and returns empty), so
   a business with no fields — or a project where the table doesn't exist yet — gets the join flow
   it has today, step for step. It needs a **customer-readable** policy: any signed-in
   customer who can see a business's services has to be able to see the questions those services
   ask, or they can't be asked them. It exposes nothing about anyone's answers.
2. Per service, for the owner's editor:
   `GET /service_intake_fields?select=*&service_id=eq.<id>&order=sort_order.asc`

**Writes the front end makes** (owner only): insert, patch (`field_type`, `label`, `is_required`,
`options`), patch of `sort_order` alone for reordering, and delete. All should sit behind the same
"owns the business this service belongs to" check the existing `services` policies use.

## 2. `intake_responses` jsonb column

A new nullable `jsonb` column on **both** `queue_entries` and `bookings`. One column, one write, as
part of the entry/booking creation that already happens — not a second table and not a second
round-trip.

Shape: an object keyed by `service_intake_fields.id`, each value a snapshot of both the question and
the answer:

```json
{
  "6f5f…": {
    "label": "What's the main thing you're coming in for?",
    "field_type": "short_text",
    "sort_order": 0,
    "is_required": true,
    "value": "Repeat script for my mother"
  },
  "9a21…": {
    "label": "Anything we should be careful of?",
    "field_type": "multi_select",
    "sort_order": 2,
    "is_required": false,
    "values": ["Allergies"]
  },
  "c704…": {
    "label": "Script, referral or a photo — if you have one",
    "field_type": "file",
    "sort_order": 3,
    "is_required": false,
    "file": { "path": "<auth.uid()>/<service>/<field>/<uuid>.pdf", "name": "script.pdf",
              "content_type": "application/pdf", "size_bytes": 51201 }
  }
}
```

Front-end stub: `IntakeAnswer` (`QueueApp/Services/Api/Intake/Models/IntakeAnswer.cs`).

**Why the label is stored with the answer** — this was the open product question in §7 of the intent
file, and it was settled before the visit page was built: **stored answers are a snapshot.** The
label, type and order are written alongside the value, and the visit detail page renders straight
out of the jsonb without ever reading `service_intake_fields`. A shop can rename a question, reorder
it or delete it a month later and every visit that answered the old question still shows the old
question. Two consequences for this spec:

- No FK is needed from the jsonb keys back to `service_intake_fields`, and deleting a field must
  **not** touch stored answers.
- Every defined field is written at submit time, including optional ones the customer left blank
  (their `value`/`values`/`file` simply absent). That is deliberate: the operator has to be able to
  tell "asked, not answered" from "never asked".

**Reads:** both are picked up automatically — the visit page's queue and booking reads already
select `*`, so nothing in `IQueueApi`/`IBookingApi` changes. Front-end stubs carrying the column:
`MyQueueEntryResponse` and `UpcomingBookingResponse`.

**RLS:** answers are the customer's own data and the shop's working information. The existing
visibility rules on `queue_entries` and `bookings` — customer sees their own row, owner/operator
sees their business's rows — are the right ones; this column just needs to ride along with them and
must not widen anything.

## 3. RPC parameters

Three existing functions need one more parameter each. All three are **omitted from the request
body entirely** unless the service actually defined fields, so every join and booking made today
sends exactly the payload it sends now:

| Function | New parameter | Front-end stub |
|---|---|---|
| `join_queue` | `p_intake_responses jsonb default null` | `JoinQueueRequest.IntakeResponses` |
| `create_booking` | `p_intake_responses jsonb default null` | `CreateBookingRequest.IntakeResponses` |
| `create_booking_any` | `p_intake_responses jsonb default null` | `CreateBookingAnyRequest.IntakeResponses` |

Each should write its argument straight into the new column on the row it creates. Defaulting to
null keeps every existing caller valid.

The shop's own counter booking is a direct insert rather than an RPC, so it writes
`bookings.intake_responses` as a column (`CreateOperatorBookingRequest.IntakeResponses`) and needs
the insert policy to accept it.

**Validation:** required-field enforcement is a front-end rule today (the intake step's CTA stays
off until every required field is answered). If the backend wants that guarantee too, these
functions are where it belongs — nothing else on the write path sees both the definitions and the
answers.

## 4. File storage — a genuine gap

**The front end expects a private `intake-uploads` Supabase Storage bucket.** File-upload fields
cannot ship until selected files are uploaded to it. What still has to be decided:

- **Bucket name and privacy.** The front end writes the storage object key
  `<auth.uid()>/<service_id>/<field_id>/<uuid><ext>`. The bucket name remains a backend decision.
  Private is the only defensible default: a prescription is the motivating case for this whole
  feature.
- **Who may upload.** The customer, before the entry exists — so the upload happens ahead of any row
  that could be used to authorise it. The first path segment is the uploader's user id, allowing
  the storage policy to authorise the upload with `auth.uid() = (storage.foldername(name))[1]`.
- **Who may read.** The customer who uploaded it, and the business that was asked to act on it.
  There is no obvious existing policy to copy for the second half.
- **Retention.** These are medical-adjacent documents in the motivating case. How long they live
  after the visit settles is a policy question, not a technical one.
- **Limits.** Max size and accepted types. The picker is currently restricted to images and PDFs
  front-end-side; that restriction is not enforcement.

`IntakeFileService` uploads the picked file before returning its reference. The visit page downloads
the object through the authenticated Supabase Storage endpoint and opens its cached local copy
(`VisitPageViewModel.OpenIntakeFileAsync`).

## 5. What was deliberately not built

- **No migration or RLS policy is included here.** The `intake-uploads` bucket and policies must be
  created in Supabase before uploads and downloads can succeed.
- **No new field types.** Five, as specified — date, number and the rest wait for a real use case.
- **Nothing downstream was touched.** The confirm step, queue engine, position tracking, realtime,
  notifications, operator queue board and booking agenda are all unmodified. The only existing
  sequencing logic that changed is `FlowStepEngine.BuildSteps`, which takes one more optional
  argument and inserts one step when it is true.
- **No draft persistence.** Answers live in the flow's view model for as long as the flow does.
- **The board's walk-in sheet doesn't ask.** A walk-in added from the operator queue board goes
  through `AddWalkInAsync` and writes no `intake_responses`. That sheet is part of the operator
  queue board, which §2 of the intent file put out of scope; if the shop should be able to answer a
  service's questions on a customer's behalf there, that is a follow-up.

## 6. Open question for whoever picks this up

The intent file's §6 says the answers surface on the **visit detail page** so the operator can read
them. In the app as it stands, the visit detail page (`VisitPage`) is customer-facing — it is opened
from the join flow, the business landing, History and the browse dashboard, all customer entry
points. There is no operator-side visit detail page to put the section on, and the operator queue
board and booking agenda are explicitly out of scope (§2).

So the section is rendered where the spec says, and today that means the customer can see what they
answered but **the operator still has no screen that shows it**. Closing that needs either an
operator-side visit detail page or an explicit decision to surface it in one of the two screens this
pass was told not to touch. Flagging rather than resolving, per §8.4.
