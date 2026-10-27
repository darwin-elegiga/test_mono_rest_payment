-- ============================
-- CDC DDL FOR SETTLE DETAIL REPORT (DUPLICATE SAFE)
-- ============================
-- Keep this append-only. Do not modify prior executed changelog files.

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_publication
        WHERE pubname = 'vc_reporting_pub'
    ) THEN
        CREATE PUBLICATION vc_reporting_pub
        FOR TABLE vc_reporting.settle_detail_report;
    ELSE
        BEGIN
            ALTER PUBLICATION vc_reporting_pub
            ADD TABLE vc_reporting.settle_detail_report;
        EXCEPTION
            -- Partitioned-table membership can already exist even when
            -- pg_publication_tables does not surface the parent relation.
            WHEN duplicate_object THEN
                NULL;
        END;
    END IF;
END
$$;
