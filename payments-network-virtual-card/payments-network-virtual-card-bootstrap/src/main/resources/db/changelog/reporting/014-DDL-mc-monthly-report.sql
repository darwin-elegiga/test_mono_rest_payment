CREATE TABLE IF NOT EXISTS vc_reporting.mc_monthly_report (
    report_month            date          NOT NULL,
    bin                     varchar(10)   NOT NULL,
    org_short_code          varchar(20)   NOT NULL,
    account_short_code      varchar(20)   NOT NULL,
    sub_account_short_code  varchar(20)   NOT NULL,
    sub_account_name        varchar(250)  NULL,
    total_amount            numeric(20,2) NOT NULL DEFAULT 0,
    total_interchange_amount numeric(20,2) NOT NULL DEFAULT 0,
    CONSTRAINT pk_mc_monthly_report PRIMARY KEY (
        report_month, bin, org_short_code, account_short_code, sub_account_short_code
    )
);

-- No secondary indexes: the composite PK already serves the upsert's ON CONFLICT
-- target and any report_month-prefixed lookups; add column-specific indexes only
-- once a real query path (e.g. a reporting API) needs them.
