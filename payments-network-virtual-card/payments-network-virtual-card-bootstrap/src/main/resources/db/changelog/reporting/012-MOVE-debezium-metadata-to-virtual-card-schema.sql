-- ==================================================
-- MOVE Debezium metadata tables out of reporting schema
-- ==================================================
-- Append-only changeset. Keep Debezium operational metadata outside
-- the captured reporting schema so publication management stays scoped
-- to business CDC tables.

CREATE SCHEMA IF NOT EXISTS virtual_card;

DO $$
BEGIN
    IF to_regclass('vc_reporting.debezium_offset_storage') IS NOT NULL
       AND to_regclass('virtual_card.debezium_offset_storage') IS NULL THEN
        EXECUTE 'ALTER TABLE vc_reporting.debezium_offset_storage SET SCHEMA virtual_card';
    ELSIF to_regclass('vc_reporting.debezium_offset_storage') IS NOT NULL
          AND to_regclass('virtual_card.debezium_offset_storage') IS NOT NULL THEN
        INSERT INTO virtual_card.debezium_offset_storage (id, offset_key, offset_val, record_insert_ts, record_insert_seq)
        SELECT id, offset_key, offset_val, record_insert_ts, record_insert_seq
        FROM vc_reporting.debezium_offset_storage
        ON CONFLICT (id) DO UPDATE
        SET offset_key = EXCLUDED.offset_key,
            offset_val = EXCLUDED.offset_val,
            record_insert_ts = EXCLUDED.record_insert_ts,
            record_insert_seq = EXCLUDED.record_insert_seq;

        EXECUTE 'DROP TABLE vc_reporting.debezium_offset_storage';
    END IF;

    IF to_regclass('vc_reporting.debezium_database_history') IS NOT NULL
       AND to_regclass('virtual_card.debezium_database_history') IS NULL THEN
        EXECUTE 'ALTER TABLE vc_reporting.debezium_database_history SET SCHEMA virtual_card';
    ELSIF to_regclass('vc_reporting.debezium_database_history') IS NOT NULL
          AND to_regclass('virtual_card.debezium_database_history') IS NOT NULL THEN
        INSERT INTO virtual_card.debezium_database_history (id, history_data, history_data_seq, record_insert_ts, record_insert_seq)
        SELECT id, history_data, history_data_seq, record_insert_ts, record_insert_seq
        FROM vc_reporting.debezium_database_history
        ON CONFLICT (id, history_data_seq) DO UPDATE
        SET history_data = EXCLUDED.history_data,
            record_insert_ts = EXCLUDED.record_insert_ts,
            record_insert_seq = EXCLUDED.record_insert_seq;

        EXECUTE 'DROP TABLE vc_reporting.debezium_database_history';
    END IF;

    IF to_regclass('vc_reporting.debezium_heartbeat') IS NOT NULL
       AND to_regclass('virtual_card.debezium_heartbeat') IS NULL THEN
        EXECUTE 'ALTER TABLE vc_reporting.debezium_heartbeat SET SCHEMA virtual_card';
    ELSIF to_regclass('vc_reporting.debezium_heartbeat') IS NOT NULL
          AND to_regclass('virtual_card.debezium_heartbeat') IS NOT NULL THEN
        INSERT INTO virtual_card.debezium_heartbeat (id, last_heartbeat_at)
        SELECT id, last_heartbeat_at
        FROM vc_reporting.debezium_heartbeat
        ON CONFLICT (id) DO UPDATE
        SET last_heartbeat_at = EXCLUDED.last_heartbeat_at;

        EXECUTE 'DROP TABLE vc_reporting.debezium_heartbeat';
    END IF;
END
$$;

CREATE TABLE IF NOT EXISTS virtual_card.debezium_offset_storage (
    id                varchar(36)   NOT NULL,
    offset_key        varchar(1255) NULL,
    offset_val        varchar(1255) NULL,
    record_insert_ts  timestamp     NOT NULL,
    record_insert_seq integer       NOT NULL,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS virtual_card.debezium_database_history (
    id                varchar(36)    NOT NULL,
    history_data      varchar(65000) NULL,
    history_data_seq  integer        NULL,
    record_insert_ts  timestamp      NOT NULL,
    record_insert_seq integer        NOT NULL,
    PRIMARY KEY (id, history_data_seq)
);

CREATE TABLE IF NOT EXISTS virtual_card.debezium_heartbeat (
    id                integer   NOT NULL,
    last_heartbeat_at timestamp NOT NULL,
    PRIMARY KEY (id)
);

INSERT INTO virtual_card.debezium_heartbeat (id, last_heartbeat_at)
VALUES (1, clock_timestamp())
ON CONFLICT (id) DO NOTHING;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_roles
        WHERE rolname = 'virtual_card_cdc_user'
    ) THEN
        EXECUTE 'GRANT USAGE ON SCHEMA virtual_card TO virtual_card_cdc_user';
        EXECUTE 'GRANT SELECT, INSERT, UPDATE ON TABLE virtual_card.debezium_heartbeat TO virtual_card_cdc_user';
        EXECUTE 'GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE virtual_card.debezium_offset_storage TO virtual_card_cdc_user';
        EXECUTE 'GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE virtual_card.debezium_database_history TO virtual_card_cdc_user';
    END IF;
END
$$;