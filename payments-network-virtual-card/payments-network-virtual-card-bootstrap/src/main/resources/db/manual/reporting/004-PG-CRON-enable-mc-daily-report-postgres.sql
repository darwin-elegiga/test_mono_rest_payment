-- Run this script while connected to the postgres database.
-- Keep this step outside Liquibase because Liquibase for this service
-- connects to virtual-card, while pg_cron metadata is owned by postgres.
-- This script is idempotent and is intended as a one-time per-environment
-- setup unless the PostgreSQL server or cron metadata is recreated.

CREATE EXTENSION IF NOT EXISTS pg_cron;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM cron.job
        WHERE jobname = 'vc_mc_daily_report_2pm'
          AND database = 'virtual-card'
    ) THEN
        PERFORM cron.schedule_in_database(
            'vc_mc_daily_report_2pm',
            '0 14 * * *',
            'CALL vc_reporting.load_mc_daily_report();',
            'virtual-card'
        );
    END IF;
END $$;