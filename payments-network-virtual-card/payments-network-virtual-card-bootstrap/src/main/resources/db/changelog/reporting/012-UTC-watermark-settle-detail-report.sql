-- ============================
-- NORMALIZE REPORTING WATERMARKS TO UTC
-- ============================
-- Purpose:
-- Ensure manual or scheduled runs use the same UTC wall-clock basis when
-- writing job_run_log timestamps and incremental watermarks.

ALTER TABLE vc_reporting.job_run_log
    ALTER COLUMN started_at SET DEFAULT (clock_timestamp() AT TIME ZONE 'UTC');

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

    INSERT INTO vc_reporting.job_run_log (job_name, status, started_at)
    VALUES (v_job_name, 'STARTED', clock_timestamp() AT TIME ZONE 'UTC');

    SELECT COALESCE(max(last_success_watermark), '1970-01-01'::timestamp)
      INTO v_last_watermark
      FROM vc_reporting.job_run_log
     WHERE job_name = v_job_name
       AND status = 'SUCCESS';

    v_ceiling := (clock_timestamp() AT TIME ZONE 'UTC') - interval '5 seconds';

    WITH source_rows AS (
        SELECT
            ce.id AS clear_extract_id,
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
          AND COALESCE(ce.updated_at, ce.created_at) <= v_ceiling
    )
    INSERT INTO vc_reporting.settle_detail_report (
        clear_extract_id,
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
        source_rows.clear_extract_id,
        source_rows.org_short_code,
        source_rows.account_short_code,
        source_rows.sub_account_short_code,
        source_rows.payment_id,
        source_rows.claim_number,
        source_rows.amount,
        source_rows.reason_code,
        source_rows.status,
        source_rows.txn_timestamp
    FROM source_rows
    WHERE NOT EXISTS (
        SELECT 1
        FROM vc_reporting.settle_detail_report existing
        WHERE existing.clear_extract_id = source_rows.clear_extract_id
    );

    GET DIAGNOSTICS v_rows = ROW_COUNT;

    UPDATE vc_reporting.job_run_log
       SET status = 'SUCCESS',
           ended_at = clock_timestamp() AT TIME ZONE 'UTC',
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
               ended_at = clock_timestamp() AT TIME ZONE 'UTC',
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