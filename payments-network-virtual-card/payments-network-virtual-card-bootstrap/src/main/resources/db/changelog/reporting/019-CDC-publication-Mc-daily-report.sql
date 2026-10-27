-- ============================
-- CDC DDL FOR MC DAILY REPORT
-- ============================

ALTER TABLE vc_reporting.mc_daily_report
REPLICA IDENTITY FULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_publication
        WHERE pubname = 'vc_reporting_pub'
    ) THEN
        CREATE PUBLICATION vc_reporting_pub
        FOR TABLE vc_reporting.mc_daily_report;
    ELSIF NOT EXISTS (
        SELECT 1
        FROM pg_publication_tables
        WHERE pubname = 'vc_reporting_pub'
          AND schemaname = 'vc_reporting'
          AND tablename = 'mc_daily_report'
    ) THEN
        ALTER PUBLICATION vc_reporting_pub
        ADD TABLE vc_reporting.mc_daily_report;
    END IF;
END
$$;
