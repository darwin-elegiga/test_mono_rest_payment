-- ============================
-- PG_CRON SETUP FOR SETTLE DETAIL REPORT
-- This changelog runs in the virtual-card database.
-- cron.database_name must be set to virtual-card so pg_cron metadata
-- and job execution both live in the same database.
-- ============================

-- CREATE EXTENSION IF NOT EXISTS pg_cron;

--SELECT cron.schedule(
--    'vc_settle_detail_report_5min',
--    '*/5 * * * *',
--    $$CALL vc_reporting.load_settle_detail_report();$$
--)
--WHERE NOT EXISTS (
--    SELECT 1
--    FROM cron.job
--    WHERE jobname = 'vc_settle_detail_report_5min'
--);
