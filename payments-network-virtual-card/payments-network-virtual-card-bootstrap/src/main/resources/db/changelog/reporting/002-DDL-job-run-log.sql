-- ============================
-- CONTROL TABLE
-- ============================
-- Purpose:
-- This table tracks every run of the reporting load job.
-- It serves as:
-- 1) A watermark store (last_success_watermark) so incremental loads only pick new data.
-- 2) An audit log (status, timestamps, row counts, errors).
-- 3) A concurrency guard (detects if another run is already active).

CREATE TABLE IF NOT EXISTS vc_reporting.job_run_log (
    job_name               varchar(100) NOT NULL,
    run_id                 bigserial PRIMARY KEY,
    status                 varchar(20)  NOT NULL,  -- STARTED | SUCCESS | FAILED | SKIPPED
    started_at             timestamp    NOT NULL DEFAULT now(),
    ended_at               timestamp    NULL,
    rows_inserted          integer      NULL,
    last_success_watermark timestamp    NULL,
    error_message          text         NULL
);


