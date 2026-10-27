CREATE SCHEMA IF NOT EXISTS vc_reporting;

CREATE TABLE IF NOT EXISTS vc_reporting.settle_detail_report (
    org_short_code         varchar(20)   NOT NULL,
    account_short_code     varchar(20)   NOT NULL,
    sub_account_short_code varchar(20)   NOT NULL,
    payment_id             varchar(32)   NOT NULL,
    claim_number           varchar(50)   NULL,
    amount                 numeric(20,2) NOT NULL,
    reason_code            varchar(200)  NULL,
    status                 varchar(20)   NOT NULL,
    txn_timestamp          timestamp     NOT NULL
) PARTITION BY RANGE (txn_timestamp);


-- ============================
-- Partition vc_reporting.settle_detail_report by txn_timestamp (monthly)
-- NOTE: Intended to run before initial load.
-- ============================

CREATE TABLE vc_reporting.settle_detail_report_default
    PARTITION OF vc_reporting.settle_detail_report
    DEFAULT;

-- Create monthly partitions for the current month and the next 10 years
DO $$
DECLARE
    v_start date := date_trunc('month', current_date)::date;
    v_end date := (date_trunc('month', current_date) + interval '121 months')::date;
    v_iter date;
    v_next date;
    v_partition text;
BEGIN
    v_iter := v_start;
    WHILE v_iter < v_end LOOP
        v_next := (v_iter + interval '1 month')::date;
        v_partition := format('vc_reporting.settle_detail_report_%s', to_char(v_iter, 'yyyymm'));
        EXECUTE format(
            'CREATE TABLE IF NOT EXISTS %s PARTITION OF vc_reporting.settle_detail_report FOR VALUES FROM (%L) TO (%L);',
            v_partition,
            v_iter::timestamp,
            v_next::timestamp
        );
        v_iter := v_next;
    END LOOP;
END $$;

CREATE INDEX IF NOT EXISTS idx_settle_detail_report_org
    ON vc_reporting.settle_detail_report (org_short_code);

CREATE INDEX IF NOT EXISTS idx_settle_detail_report_account
    ON vc_reporting.settle_detail_report (account_short_code);

CREATE INDEX IF NOT EXISTS idx_settle_detail_report_sub_account
    ON vc_reporting.settle_detail_report (sub_account_short_code);

CREATE INDEX IF NOT EXISTS idx_settle_detail_report_payment
    ON vc_reporting.settle_detail_report (payment_id);

CREATE INDEX IF NOT EXISTS idx_settle_detail_report_ts
    ON vc_reporting.settle_detail_report (txn_timestamp);
