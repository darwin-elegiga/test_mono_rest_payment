-- =============================================================
-- Normalize CDC publication membership to current reporting tables
-- =============================================================
-- Append-only changeset. Do not modify prior reporting changelog files.
--
-- Historical changesets enrolled settle_detail_report explicitly. PostgreSQL
-- schema-level publication management (`FOR TABLES IN SCHEMA` / `SET TABLES IN
-- SCHEMA`) requires superuser privileges, which are not available on Azure
-- Flexible Server. Instead, bootstrap creates the publication once and adds
-- each current business table in vc_reporting individually.

DO $$
DECLARE
    reporting_table RECORD;
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_publication
        WHERE pubname = 'vc_reporting_pub'
    ) THEN
        CREATE PUBLICATION vc_reporting_pub;
    END IF;

    FOR reporting_table IN
        SELECT tablename
        FROM pg_tables
        WHERE schemaname = 'vc_reporting'
                    AND tablename NOT LIKE 'debezium\_%' ESCAPE '\'
        ORDER BY tablename
    LOOP
        BEGIN
            EXECUTE format(
                'ALTER PUBLICATION vc_reporting_pub ADD TABLE %I.%I',
                'vc_reporting',
                reporting_table.tablename
            );
        EXCEPTION
            WHEN duplicate_object THEN
                NULL;
        END;
    END LOOP;
END
$$;