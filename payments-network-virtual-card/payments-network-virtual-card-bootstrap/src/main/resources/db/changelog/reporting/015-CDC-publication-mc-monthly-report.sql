-- ============================
-- CDC DDL FOR MC MONTHLY REPORT (DUPLICATE SAFE)
-- ============================
-- Keep this append-only. Do not modify prior executed changelog files.

ALTER TABLE vc_reporting.mc_monthly_report
REPLICA IDENTITY FULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_publication
        WHERE pubname = 'vc_reporting_pub'
    ) THEN
        CREATE PUBLICATION vc_reporting_pub
        FOR TABLE vc_reporting.mc_monthly_report;
    ELSE
        BEGIN
            ALTER PUBLICATION vc_reporting_pub
            ADD TABLE vc_reporting.mc_monthly_report;
        EXCEPTION
            WHEN duplicate_object THEN
                NULL;
        END;
    END IF;
END
$$;
