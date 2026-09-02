-- Step 17 — queue mode assigns an operator at join time.
--
-- Before this, a null p_operator_id meant "shared pool", and the pool is a poor place for a
-- customer to sit: compute_wait_minutes matches operator_id with `is not distinct from`, so a
-- pooled entry only counts other pooled entries and reads ~0 minutes, while my_queue_status
-- ranks `partition by business_id, operator_id` and calls them 1st. First in line, no wait,
-- behind nobody — none of it true.
--
-- Now a null p_operator_id means "pick for me": the operator with the shortest projected wait,
-- ties broken by who has fewer people waiting and then by the shop's own display order. The pool
-- survives as the honest fallback for a shop with nobody on shift, and as the board's deliberate
-- destination when it un-assigns someone (a direct PATCH on operator_id, not this function).

-- 1. join_queue --------------------------------------------------------------------------------
--
-- Dropped, not just replaced: `create or replace` keys on the argument list, so adding
-- p_auto_assign would leave the six-argument version standing beside the new one. The app sends
-- exactly six named arguments, which then matches both — and PostgREST gets back
-- "function join_queue(...) is not unique" on every single join. Drop first.

drop function if exists public.join_queue(uuid, uuid, uuid, uuid, text, jsonb);

create or replace function public.join_queue(
  p_business_id   uuid,
  p_operator_id   uuid    default null::uuid,
  p_service_id    uuid    default null::uuid,
  p_customer_id   uuid    default null::uuid,
  p_customer_name text    default null::text,
  p_details       jsonb   default null::jsonb,
  p_auto_assign   boolean default true
)
returns queue_entries
language plpgsql
security definer
set search_path to 'public'
as $function$
declare
  v_row      public.queue_entries;
  v_caller   uuid  := auth.uid();
  v_operator uuid  := p_operator_id;
  v_details  jsonb := p_details;
begin
  -- Permission: either the caller is adding THEMSELVES, or the caller owns the business (counter add).
  if not (
    (p_customer_id is not null and p_customer_id = v_caller)
    or public.is_business_owner(p_business_id)
  ) then
    raise exception 'not allowed to add to this queue';
  end if;

  -- Guard: don't let the same logged-in customer sit in the same business queue twice while waiting.
  if p_customer_id is not null and exists (
    select 1 from public.queue_entries
    where business_id = p_business_id and customer_id = p_customer_id and status = 'waiting'
  ) then
    raise exception 'customer already in this queue';
  end if;

  -- No operator asked for, and nobody has said "leave this one unassigned" — pick the shortest wait.
  if v_operator is null and p_auto_assign then
    -- Serialise joins per business. Two customers a millisecond apart would otherwise both read
    -- the same pre-join queue and both land on the same operator. Transaction-scoped, held for
    -- the length of one insert, and only ever contended by joins at the same shop.
    perform pg_advisory_xact_lock(hashtextextended(p_business_id::text, 0));

    select o.id
      into v_operator
    from public.operators o
    where o.business_id = p_business_id
      and o.is_active = true
      and o.is_available = true
    order by
      public.compute_wait_minutes(p_business_id, o.id, null::timestamptz) asc,
      (
        select count(*)
        from public.queue_entries q
        where q.operator_id = o.id
          and q.status = 'waiting'
      ) asc,
      o.sort_order asc,
      o.id asc
    limit 1;

    -- Stamped so the board can tell an auto-pick from a customer's deliberate choice, and shuffle
    -- the auto ones without second-guessing whether someone asked for that chair by name.
    if v_operator is not null then
      v_details := coalesce(v_details, '{}'::jsonb) || '{"assigned":"auto"}'::jsonb;
    end if;
  end if;

  insert into public.queue_entries (
    business_id, operator_id, service_id, customer_id, customer_name, details, status
  )
  values (
    p_business_id, v_operator, p_service_id, p_customer_id, p_customer_name, v_details, 'waiting'
  )
  returning * into v_row;

  return v_row;
end;
$function$;

-- 2. my_queue_status ---------------------------------------------------------------------------
--
-- Two changes, both about the entry that is still genuinely unassigned (nobody on shift):
--
--   * Position was `partition by business_id, operator_id`, which gives the pool its own private
--     ranking and calls a pooled customer 1st while seven people sit ahead of them. Unassigned
--     entries now count everyone ahead of them in the business, which is what my_active_queue_entry
--     already does — the two screens disagreed by design.
--   * The hardcoded 'Any available' is gone. The app owns that wording (it has to say something
--     different on three screens), and it can't tell a real operator called nothing from a null.

create or replace function public.my_queue_status(p_business_id uuid)
returns table(
  entry_id uuid,
  operator_id uuid,
  operator_name text,
  queue_position integer,
  status text,
  progress_status text,
  joined_at timestamp with time zone
)
language sql
stable
security definer
set search_path to 'public'
as $function$
  select entry_id, operator_id, operator_name, queue_position, status, progress_status, joined_at
  from (
    select
      q.id as entry_id,
      q.operator_id,
      o.display_name as operator_name,
      case
        when q.operator_id is not null then
          row_number() over (
            partition by q.business_id, q.operator_id
            order by q.joined_at
          )::int
        else
          (
            select count(*)::int
            from public.queue_entries a
            where a.business_id = q.business_id
              and a.status in ('waiting', 'serving')
              and a.joined_at < q.joined_at
          ) + 1
      end as queue_position,
      q.status,
      q.progress_status,
      q.joined_at,
      q.customer_id
    from public.queue_entries q
    left join public.operators o on o.id = q.operator_id
    where q.business_id = p_business_id
      and q.status in ('waiting', 'serving')
  ) ranked
  where ranked.customer_id = auth.uid()
  order by ranked.joined_at desc
  limit 1;
$function$;

-- 3. PostgREST ---------------------------------------------------------------------------------
-- The schema cache holds the old signature until told otherwise; without this the first join
-- after the migration can still 404 on the dropped function.

notify pgrst, 'reload schema';
