-- ==================================================
-- GRANT CDC role access after reporting schema exists
-- ==================================================
-- Append-only changeset. Do not modify prior reporting changelog files.
--
-- The infra-time CDC role job can run before bootstrap creates vc_reporting.
-- When that happens, the CDC role exists but misses schema-scoped grants.
-- Bootstrap owns the reporting schema and tables, so reconcile those grants
-- here once the schema exists.

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_roles
        WHERE rolname = 'virtual_card_cdc_user'
    ) THEN
        EXECUTE 'GRANT USAGE ON SCHEMA vc_reporting TO virtual_card_cdc_user';
        EXECUTE 'GRANT SELECT ON ALL TABLES IN SCHEMA vc_reporting TO virtual_card_cdc_user';
        EXECUTE 'GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA vc_reporting TO virtual_card_cdc_user';
        EXECUTE 'ALTER DEFAULT PRIVILEGES IN SCHEMA vc_reporting GRANT SELECT ON TABLES TO virtual_card_cdc_user';
        EXECUTE 'ALTER DEFAULT PRIVILEGES IN SCHEMA vc_reporting GRANT USAGE, SELECT ON SEQUENCES TO virtual_card_cdc_user';
    END IF;
END
$$;