-- ============================
-- SET INITIAL WATERMARK
-- ============================
INSERT INTO vc_reporting.job_run_log (
    job_name, status, started_at, ended_at, rows_inserted, last_success_watermark
)
SELECT
    'settle_detail_report',
    'SUCCESS',
    clock_timestamp(),
    clock_timestamp(),
    (SELECT count(*) FROM vc_reporting.settle_detail_report),
    (SELECT max(COALESCE(updated_at, created_at)) FROM virtual_card.clear_extract)
WHERE NOT EXISTS (
    SELECT 1
    FROM vc_reporting.job_run_log
    WHERE job_name = 'settle_detail_report'
      AND status = 'SUCCESS'
);