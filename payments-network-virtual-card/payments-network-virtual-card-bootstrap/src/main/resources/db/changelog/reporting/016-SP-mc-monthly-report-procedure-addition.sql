-- ============================
-- FIX MC MONTHLY REPORT TRANSACTION CONTROL
-- ============================
-- Purpose:
-- Replace the original procedure implementation so month-by-month commits
-- remain supported without issuing COMMIT from inside a PL/pgSQL block that
-- has an EXCEPTION handler.
--
-- PostgreSQL forbids COMMIT/ROLLBACK while an EXCEPTION-backed subtransaction
-- is active. This version isolates per-month work in an inner block, captures
-- any failure, writes FAILED status outside that block, commits the failure
-- log, releases the advisory lock, and then re-raises the original error.
--
-- Idempotency / rerun-safety:
-- The INSERT uses ON CONFLICT ... DO UPDATE SET so that if an operator resets
-- last_success_watermark and reruns the procedure (e.g. to absorb late-arriving
-- or corrected clear_extract rows for an already-processed month), the aggregate
-- totals in mc_monthly_report are recalculated rather than silently skipped.
-- Normal forward-only runs are unaffected: the watermark advances to v_month_end
-- on each SUCCESS commit, so each closed month is processed exactly once unless
-- an operator intentionally rewinds the watermark.
-- rows_inserted reflects the number of rows affected by the upsert (inserts +
-- updates) as reported by GET DIAGNOSTICS ROW_COUNT.

CREATE OR REPLACE PROCEDURE vc_reporting.load_mc_monthly_report()
LANGUAGE plpgsql
AS $$
DECLARE
    v_job_name text := 'mc_monthly_report';
    v_month_start timestamp;
    v_month_end timestamp;
    v_current_month_start timestamp;
    v_rows integer := 0;
    v_invalid_key_rows integer := 0;
    v_months_processed integer := 0;
    v_lock_acquired boolean;
    v_run_id bigint;
    v_error_message text;
    v_error_detail text;
    v_error_hint text;
    v_error_context text;
    v_error_sqlstate text;
BEGIN
    -- Prevent concurrent runs
    v_lock_acquired := pg_try_advisory_lock(hashtext(v_job_name));
    IF NOT v_lock_acquired THEN
        INSERT INTO vc_reporting.job_run_log (job_name, status, started_at, ended_at, error_message)
        VALUES (
            v_job_name,
            'SKIPPED',
            clock_timestamp() AT TIME ZONE 'UTC',
            clock_timestamp() AT TIME ZONE 'UTC',
            'Another instance already running'
        );
        RETURN;
    END IF;

    LOOP
        -- Next month to process = the month right after the last one successfully closed.
        -- Defaults to 2025-01-01 (already month-aligned) when this has never run before.
        SELECT COALESCE(max(last_success_watermark), '2025-01-01'::timestamp)
          INTO v_month_start
          FROM vc_reporting.job_run_log
         WHERE job_name = v_job_name
           AND status = 'SUCCESS';

        -- v_month_start is always day 1 (loop invariant), so this never hits the
        -- day-31-overflows-into-next-month gotcha; it correctly spans 28/29/30/31-day months.
        v_month_end := v_month_start + interval '1 month';
        v_current_month_start := date_trunc('month', clock_timestamp() AT TIME ZONE 'UTC');

        -- Stop once we reach a month that hasn't fully elapsed; never touch the
        -- in-progress current month.
        EXIT WHEN v_month_end > v_current_month_start;

        INSERT INTO vc_reporting.job_run_log (job_name, status, started_at)
        VALUES (v_job_name, 'STARTED', clock_timestamp() AT TIME ZONE 'UTC')
        RETURNING run_id INTO v_run_id;

        v_error_message := NULL;
        v_error_detail := NULL;
        v_error_hint := NULL;
        v_error_context := NULL;
        v_error_sqlstate := NULL;

        BEGIN
            -- Reject source rows missing any report key field so one bad row does
            -- not fail the full monthly insert.
            SELECT count(*)
              INTO v_invalid_key_rows
              FROM virtual_card.clear_extract ce
              JOIN virtual_card.virtual_cards vc
                ON vc.card_guid = ce.card_id
              JOIN virtual_card.payment_request pr
                ON pr.payment_details_id = vc.payment_id
             WHERE ce.clearing_date >= v_month_start
               AND ce.clearing_date < v_month_end
               AND (
                   ce.bin IS NULL
                   OR pr.org_short_code IS NULL
                   OR pr.account_short_code IS NULL
                   OR pr.sub_account_short_code IS NULL
               );

            WITH month_totals AS (
                SELECT
                    v_month_start::date AS report_month,
                    ce.bin,
                    pr.org_short_code,
                    pr.account_short_code,
                    pr.sub_account_short_code,
                    sa.name AS sub_account_name,
                    sum(
                        CASE
                            WHEN ce.settlement_indicator = 'CREDIT' THEN abs(coalesce(ce.clearing_amount, 0))
                            WHEN ce.settlement_indicator = 'DEBIT' THEN -abs(coalesce(ce.clearing_amount, 0))
                            ELSE 0
                        END
                    ) AS total_amount,
                    coalesce(sum(coalesce(ce.interchange_fee_amount, 0)), 0) AS total_interchange_amount
                FROM virtual_card.clear_extract ce
                JOIN virtual_card.virtual_cards vc
                    ON vc.card_guid = ce.card_id
                JOIN virtual_card.payment_request pr
                    ON pr.payment_details_id = vc.payment_id
                LEFT JOIN virtual_card.sub_accounts sa
                    ON sa.org_short_code = pr.org_short_code
                   AND sa.account_short_code = pr.account_short_code
                   AND sa.sub_account_short_code = pr.sub_account_short_code
                WHERE ce.clearing_date >= v_month_start
                  AND ce.clearing_date < v_month_end
                  AND ce.bin IS NOT NULL
                  AND pr.org_short_code IS NOT NULL
                  AND pr.account_short_code IS NOT NULL
                  AND pr.sub_account_short_code IS NOT NULL
                GROUP BY
                    ce.bin,
                    pr.org_short_code,
                    pr.account_short_code,
                    pr.sub_account_short_code,
                    sa.name
            )
            INSERT INTO vc_reporting.mc_monthly_report (
                report_month,
                bin,
                org_short_code,
                account_short_code,
                sub_account_short_code,
                sub_account_name,
                total_amount,
                total_interchange_amount
            )
            SELECT
                month_totals.report_month,
                month_totals.bin,
                month_totals.org_short_code,
                month_totals.account_short_code,
                month_totals.sub_account_short_code,
                month_totals.sub_account_name,
                month_totals.total_amount,
                month_totals.total_interchange_amount
            FROM month_totals
            ON CONFLICT (report_month, bin, org_short_code, account_short_code, sub_account_short_code)
            DO UPDATE SET
                total_amount              = EXCLUDED.total_amount,
                total_interchange_amount  = EXCLUDED.total_interchange_amount,
                sub_account_name          = EXCLUDED.sub_account_name;

            GET DIAGNOSTICS v_rows = ROW_COUNT;

            UPDATE vc_reporting.job_run_log
               SET status = 'SUCCESS',
                   ended_at = clock_timestamp() AT TIME ZONE 'UTC',
                   rows_inserted = v_rows,
                   error_message = CASE
                       WHEN v_invalid_key_rows > 0 THEN format(
                           'Skipped %s source row(s) with missing report key fields (bin/org_short_code/account_short_code/sub_account_short_code).',
                           v_invalid_key_rows
                       )
                       ELSE NULL
                   END,
                   last_success_watermark = v_month_end
             WHERE run_id = v_run_id;

            v_months_processed := v_months_processed + 1;
        EXCEPTION
            WHEN OTHERS THEN
                GET STACKED DIAGNOSTICS
                    v_error_message = MESSAGE_TEXT,
                    v_error_detail = PG_EXCEPTION_DETAIL,
                    v_error_hint = PG_EXCEPTION_HINT,
                    v_error_context = PG_EXCEPTION_CONTEXT,
                    v_error_sqlstate = RETURNED_SQLSTATE;
        END;

        IF v_error_message IS NOT NULL THEN
            UPDATE vc_reporting.job_run_log
               SET status = 'FAILED',
                   ended_at = clock_timestamp() AT TIME ZONE 'UTC',
                   error_message = concat_ws(E'\n', v_error_message, v_error_detail, v_error_hint, v_error_context)
             WHERE run_id = v_run_id;

            PERFORM pg_advisory_unlock(hashtext(v_job_name));
            COMMIT;

            RAISE EXCEPTION USING
                MESSAGE = v_error_message,
                DETAIL = v_error_detail,
                HINT = v_error_hint,
                ERRCODE = v_error_sqlstate;
        END IF;

        -- Persist each month's progress independently: if a later month in this
        -- same catch-up run fails, earlier months already committed here are not
        -- rolled back and won't need to be reprocessed.
        COMMIT;
    END LOOP;

    IF v_months_processed = 0 THEN
        INSERT INTO vc_reporting.job_run_log (job_name, status, started_at, ended_at, error_message)
        VALUES (
            v_job_name,
            'SKIPPED',
            clock_timestamp() AT TIME ZONE 'UTC',
            clock_timestamp() AT TIME ZONE 'UTC',
            'Target month has not fully elapsed yet: ' || to_char(v_month_start, 'YYYY-MM')
        );
    END IF;

    PERFORM pg_advisory_unlock(hashtext(v_job_name));
END;
$$;

