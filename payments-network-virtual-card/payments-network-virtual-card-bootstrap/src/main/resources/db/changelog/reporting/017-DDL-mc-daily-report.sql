CREATE SCHEMA IF NOT EXISTS vc_reporting;

CREATE TABLE IF NOT EXISTS vc_reporting.mc_daily_report (
    receipt_id             varchar(32)   NOT NULL PRIMARY KEY,
    payment_id             varchar(32)   NOT NULL,
    tran_date              Date   NOT NULL,
    tran_time              time   NOT NULL,
    amount                 numeric(20,2) NOT NULL,
    report_date            date   NOT NULL,
    created_at             timestamp     NOT NULL,
    updated_at             timestamp     NOT NULL
);


CREATE INDEX IF NOT EXISTS idx_mc_daily_report_report_date
    ON vc_reporting.mc_daily_report (report_date);


