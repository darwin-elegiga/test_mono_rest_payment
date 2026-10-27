-- =====================================================
-- CDC runtime enablement for the vc_reporting contract
-- =====================================================
-- Legacy filename note: this script used to onboard a single reporting table.
-- The CDC contract is now the whole vc_reporting schema; this manual script
-- remains only for replication slot creation because slot state is runtime
-- operational state, not schema-managed state.

-- Replication slot creation stays manual because it is operational runtime state,
-- not schema state managed by the service's standard Liquibase deployment.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_replication_slots
        WHERE slot_name = 'vc_reporting_slot'
    ) THEN
        PERFORM pg_create_logical_replication_slot('vc_reporting_slot', 'pgoutput');
    END IF;
END
$$;