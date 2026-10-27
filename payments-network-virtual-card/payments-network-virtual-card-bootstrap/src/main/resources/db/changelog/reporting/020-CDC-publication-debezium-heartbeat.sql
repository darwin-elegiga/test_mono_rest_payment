-- =============================================================
-- Add the Debezium heartbeat table to the CDC publication
-- =============================================================
-- Append-only changeset. Bootstrap/admin owns publication management;
-- virtual_card_cdc_user must not alter publication membership.

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_publication
        WHERE pubname = 'vc_reporting_pub'
    )
    AND to_regclass('virtual_card.debezium_heartbeat') IS NOT NULL THEN
        BEGIN
            ALTER PUBLICATION vc_reporting_pub
            ADD TABLE virtual_card.debezium_heartbeat;
        EXCEPTION
            WHEN duplicate_object THEN
                NULL;
        END;
    END IF;
END
$$;