-- ============================
-- DEBEZIUM SUPPORT TABLES
-- ============================

CREATE TABLE IF NOT EXISTS vc_reporting.debezium_offset_storage (
    id                varchar(36)   NOT NULL,
    offset_key        varchar(1255) NULL,
    offset_val        varchar(1255) NULL,
    record_insert_ts  timestamp     NOT NULL,
    record_insert_seq integer       NOT NULL,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS vc_reporting.debezium_database_history (
    id                varchar(36)    NOT NULL,
    history_data      varchar(65000) NULL,
    history_data_seq  integer        NULL,
    record_insert_ts  timestamp      NOT NULL,
    record_insert_seq integer        NOT NULL,
    PRIMARY KEY (id, history_data_seq)
);

CREATE TABLE IF NOT EXISTS vc_reporting.debezium_heartbeat (
    id                integer   NOT NULL,
    last_heartbeat_at timestamp NOT NULL,
    PRIMARY KEY (id)
);

INSERT INTO vc_reporting.debezium_heartbeat (id, last_heartbeat_at)
VALUES (1, clock_timestamp())
ON CONFLICT (id) DO NOTHING;