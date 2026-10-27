-- ============================
-- STORED PROCEDURE (INCREMENTAL)
-- ============================
CREATE OR REPLACE PROCEDURE vc_reporting.load_settle_detail_report()
LANGUAGE plpgsql
AS $$
DECLARE
    v_job_name text := 'settle_detail_report';
    v_last_watermark timestamp;
    v_ceiling timestamp;
    v_rows integer := 0;
    v_lock_acquired boolean;
BEGIN
    -- Prevent concurrent runs
    v_lock_acquired := pg_try_advisory_lock(hashtext(v_job_name));
    IF NOT v_lock_acquired THEN
        INSERT INTO vc_reporting.job_run_log (job_name, status, started_at, ended_at, error_message)
        VALUES (v_job_name, 'SKIPPED', clock_timestamp(), clock_timestamp(), 'Another instance already running');
        RETURN;
    END IF;

    INSERT INTO vc_reporting.job_run_log (job_name, status)
    VALUES (v_job_name, 'STARTED');

    -- Find last successful watermark
    SELECT COALESCE(max(last_success_watermark), '1970-01-01'::timestamp)
      INTO v_last_watermark
      FROM vc_reporting.job_run_log
     WHERE job_name = v_job_name
       AND status = 'SUCCESS';

    -- Ceiling prevents reading in-flight rows
    v_ceiling := clock_timestamp() - interval '5 seconds';

    INSERT INTO vc_reporting.settle_detail_report (
        org_short_code,
        account_short_code,
        sub_account_short_code,
        payment_id,
        claim_number,
        amount,
        reason_code,
        status,
        txn_timestamp
    )
    SELECT
        pr.org_short_code,
        pr.account_short_code,
        pr.sub_account_short_code,
        pr.payment_details_id AS payment_id,
        NULL::varchar AS claim_number,
        ce.clearing_amount AS amount,
        ce.failure_reason AS reason_code,
        ce.clearing_type AS status,
        ce.clearing_date AS txn_timestamp
    FROM virtual_card.clear_extract ce
    JOIN virtual_card.virtual_cards vc
        ON vc.card_guid = ce.card_id
    JOIN virtual_card.payment_request pr
        ON pr.payment_details_id = vc.payment_id
    WHERE COALESCE(ce.updated_at, ce.created_at) > v_last_watermark
      AND COALESCE(ce.updated_at, ce.created_at) <= v_ceiling;

    GET DIAGNOSTICS v_rows = ROW_COUNT;

    UPDATE vc_reporting.job_run_log
       SET status = 'SUCCESS',
           ended_at = clock_timestamp(),
           rows_inserted = v_rows,
           last_success_watermark = v_ceiling
     WHERE run_id = (
         SELECT run_id
         FROM vc_reporting.job_run_log
         WHERE job_name = v_job_name
           AND status = 'STARTED'
         ORDER BY run_id DESC
         LIMIT 1
     );

    PERFORM pg_advisory_unlock(hashtext(v_job_name));
EXCEPTION
    WHEN OTHERS THEN
        UPDATE vc_reporting.job_run_log
           SET status = 'FAILED',
               ended_at = clock_timestamp(),
               error_message = SQLERRM
         WHERE run_id = (
             SELECT run_id
             FROM vc_reporting.job_run_log
             WHERE job_name = v_job_name
               AND status = 'STARTED'
             ORDER BY run_id DESC
             LIMIT 1
         );

        PERFORM pg_advisory_unlock(hashtext(v_job_name));
        RAISE;
END;
$$;

-- ============================
-- 6) SCHEDULE EVERY 5 MINUTES
-- ============================
-- CREATE EXTENSION IF NOT EXISTS pg_cron;

-- SELECT cron.schedule(
--    'vc_settle_detail_report_5min',
--    '*/5 * * * *',
--    $$CALL vc_reporting.load_settle_detail_report();$$
-- );