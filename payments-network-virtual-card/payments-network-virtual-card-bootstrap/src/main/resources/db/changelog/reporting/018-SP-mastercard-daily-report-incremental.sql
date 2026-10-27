-- ============================
-- STORED PROCEDURE (CATCHES UP ALL BACKLOG DAYS PER RUN)
-- ============================
-- This procedure processes every fully-elapsed UTC day that has not been
-- processed yet, one day at a time in order, committing after each day:
--   [day_start 00:00:00, next_day_start 00:00:00)
-- Watermark (job_run_log.last_success_watermark) is always set to the exact
-- next-day boundary to avoid overlap/drift across runs.
CREATE OR REPLACE PROCEDURE vc_reporting.load_mc_daily_report()
LANGUAGE plpgsql
AS $$
DECLARE
    v_job_name text := 'mc_daily_report';
    v_day_start timestamp;
    v_day_end timestamp;
    v_current_day_start timestamp;
    v_rows integer := 0;
    v_days_processed integer := 0;
    v_lock_acquired boolean;
    v_failed boolean;
    v_error_message text;
BEGIN
    v_lock_acquired := pg_try_advisory_lock(hashtext(v_job_name));

    IF NOT v_lock_acquired THEN
        INSERT INTO vc_reporting.job_run_log (
            job_name,
            status,
            started_at,
            ended_at,
            error_message
        )
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
        SELECT COALESCE(max(last_success_watermark), '2025-01-01'::timestamp)
          INTO v_day_start
          FROM vc_reporting.job_run_log
         WHERE job_name = v_job_name
           AND status = 'SUCCESS';

        v_day_end := v_day_start + interval '1 day';
        v_current_day_start := date_trunc(
            'day',
            clock_timestamp() AT TIME ZONE 'UTC'
        );

        EXIT WHEN v_day_end > v_current_day_start;

        INSERT INTO vc_reporting.job_run_log (job_name, status, started_at)
        VALUES (
            v_job_name,
            'STARTED',
            clock_timestamp() AT TIME ZONE 'UTC'
        );

        v_failed := false;
        v_error_message := NULL;

        BEGIN
            WITH day_rows AS (
                SELECT
                    ae.receipt_id::varchar(32) AS receipt_id,
                    pr.payment_details_id::varchar(32) AS payment_id,
                    (ae.system_auth_time AT TIME ZONE 'UTC')::date AS tran_date,
                    (ae.system_auth_time AT TIME ZONE 'UTC')::time AS tran_time,
                    COALESCE(ae.auth_amount, 0)::numeric(20, 2) AS amount,
                    v_day_start::date AS report_date,
                    clock_timestamp() AT TIME ZONE 'UTC' AS created_at,
                    clock_timestamp() AT TIME ZONE 'UTC' AS updated_at
                FROM virtual_card.auth_extract ae
                JOIN virtual_card.virtual_cards vc
                    ON vc.card_guid = ae.card_id
                JOIN virtual_card.payment_request pr
                    ON pr.payment_details_id = vc.payment_id
                WHERE (ae.system_auth_time AT TIME ZONE 'UTC') >= v_day_start
                  AND (ae.system_auth_time AT TIME ZONE 'UTC') < v_day_end
                    AND (
                          lower(ae.updated_by) = 'auth_file_extract_processor'
                       OR lower(ae.created_by) = 'auth_file_extract_processor'
                    )
            )
            INSERT INTO vc_reporting.mc_daily_report (
                receipt_id,
                payment_id,
                tran_date,
                tran_time,
                amount,
                report_date,
                created_at,
                updated_at
            )
            SELECT
                receipt_id,
                payment_id,
                tran_date,
                tran_time,
                amount,
                report_date,
                created_at,
                updated_at
            FROM day_rows
            ON CONFLICT (receipt_id) DO NOTHING;

            GET DIAGNOSTICS v_rows = ROW_COUNT;

            UPDATE vc_reporting.job_run_log
               SET status = 'SUCCESS',
                   ended_at = clock_timestamp() AT TIME ZONE 'UTC',
                   rows_inserted = v_rows,
                   last_success_watermark = v_day_end
             WHERE run_id = (
                 SELECT run_id
                 FROM vc_reporting.job_run_log
                 WHERE job_name = v_job_name
                   AND status = 'STARTED'
                 ORDER BY run_id DESC
                 LIMIT 1
             );
        EXCEPTION
            WHEN OTHERS THEN
                v_failed := true;
                GET STACKED DIAGNOSTICS v_error_message = MESSAGE_TEXT;
        END;

        IF v_failed THEN
            UPDATE vc_reporting.job_run_log
               SET status = 'FAILED',
                   ended_at = clock_timestamp() AT TIME ZONE 'UTC',
                   error_message = v_error_message
             WHERE run_id = (
                 SELECT run_id
                 FROM vc_reporting.job_run_log
                 WHERE job_name = v_job_name
                   AND status = 'STARTED'
                 ORDER BY run_id DESC
                 LIMIT 1
             );

            COMMIT;
            PERFORM pg_advisory_unlock(hashtext(v_job_name));
            RAISE EXCEPTION '%', v_error_message;
        END IF;

        v_days_processed := v_days_processed + 1;
        COMMIT;
    END LOOP;

    IF v_days_processed = 0 THEN
        INSERT INTO vc_reporting.job_run_log (
            job_name,
            status,
            started_at,
            ended_at,
            error_message
        )
        VALUES (
            v_job_name,
            'SKIPPED',
            clock_timestamp() AT TIME ZONE 'UTC',
            clock_timestamp() AT TIME ZONE 'UTC',
            'Target day has not fully elapsed yet: '
                || to_char(v_day_start, 'YYYY-MM-DD')
        );
        COMMIT;
    END IF;

    PERFORM pg_advisory_unlock(hashtext(v_job_name));
END;
$$;