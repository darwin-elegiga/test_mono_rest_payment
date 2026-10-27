-- ===========================================================
-- Ensure replica identity before initial backfill UPDATE (007)
-- ===========================================================
DO $$
DECLARE
    v_partition regclass;
BEGIN
    EXECUTE 'ALTER TABLE vc_reporting.settle_detail_report REPLICA IDENTITY FULL';

    FOR v_partition IN
        SELECT i.inhrelid::regclass
        FROM pg_inherits i
        WHERE i.inhparent = 'vc_reporting.settle_detail_report'::regclass
    LOOP
        EXECUTE format('ALTER TABLE %s REPLICA IDENTITY FULL', v_partition);
    END LOOP;
END
$$;
