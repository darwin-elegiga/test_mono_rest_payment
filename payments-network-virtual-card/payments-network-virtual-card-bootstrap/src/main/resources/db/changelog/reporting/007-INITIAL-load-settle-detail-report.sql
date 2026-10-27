-- ============================
-- INITIAL FULL LOAD / BACKFILL
-- ============================
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
        ce.clearing_date AS txn_timestamp,
        row_number() OVER (
            PARTITION BY
                pr.org_short_code,
                pr.account_short_code,
                pr.sub_account_short_code,
                pr.payment_details_id,
                ce.clearing_amount,
                ce.failure_reason,
                ce.clearing_type,
                ce.clearing_date
            ORDER BY ce.id
        ) AS match_row_number
    FROM virtual_card.clear_extract ce
    JOIN virtual_card.virtual_cards vc
        ON vc.card_guid = ce.card_id
    JOIN virtual_card.payment_request pr
        ON pr.payment_details_id = vc.payment_id
),
target_rows AS (
    SELECT
        sdr.ctid AS target_ctid,
        sdr.org_short_code,
        sdr.account_short_code,
        sdr.sub_account_short_code,
        sdr.payment_id,
        sdr.claim_number,
        sdr.amount,
        sdr.reason_code,
        sdr.status,
        sdr.txn_timestamp,
        row_number() OVER (
            PARTITION BY
                sdr.org_short_code,
                sdr.account_short_code,
                sdr.sub_account_short_code,
                sdr.payment_id,
                sdr.amount,
                sdr.reason_code,
                sdr.status,
                sdr.txn_timestamp
            ORDER BY sdr.ctid
        ) AS match_row_number
    FROM vc_reporting.settle_detail_report sdr
    WHERE sdr.clear_extract_id IS NULL
)
UPDATE vc_reporting.settle_detail_report AS target
SET clear_extract_id = source_rows.clear_extract_id
FROM target_rows
JOIN source_rows
    ON source_rows.org_short_code = target_rows.org_short_code
   AND source_rows.account_short_code = target_rows.account_short_code
   AND source_rows.sub_account_short_code = target_rows.sub_account_short_code
   AND source_rows.payment_id = target_rows.payment_id
   AND source_rows.claim_number IS NOT DISTINCT FROM target_rows.claim_number
   AND source_rows.amount IS NOT DISTINCT FROM target_rows.amount
   AND source_rows.reason_code IS NOT DISTINCT FROM target_rows.reason_code
   AND source_rows.status = target_rows.status
   AND source_rows.txn_timestamp = target_rows.txn_timestamp
   AND source_rows.match_row_number = target_rows.match_row_number
WHERE target.ctid = target_rows.target_ctid;

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
WHERE NOT EXISTS (
    SELECT 1
    FROM vc_reporting.settle_detail_report existing
    WHERE existing.clear_extract_id = ce.id
);