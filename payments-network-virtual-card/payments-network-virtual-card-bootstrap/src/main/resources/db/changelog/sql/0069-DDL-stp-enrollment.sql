CREATE TABLE IF NOT EXISTS virtual_card.stp_enrollment_batch (
    id                  BIGSERIAL PRIMARY KEY,
    correlation_id      VARCHAR(100) NOT NULL UNIQUE,
    network_partner     VARCHAR(100),
    requested_count     INT,
    enrollment_count    INT          DEFAULT 0,
    valid_count         INT          DEFAULT 0,
    error_count         INT          DEFAULT 0,
    status              VARCHAR(50)  NOT NULL,
    callback_status     VARCHAR(50),
    preferencing_status VARCHAR(50),
    created_at          TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS virtual_card.stp_enrollment_record (
    id                            BIGSERIAL PRIMARY KEY,
    stp_enrollment_batch_id       BIGINT       NOT NULL,
    provider_tin                  VARCHAR(20)  NOT NULL,
    legal_entity_name             VARCHAR(255),
    doing_business_as             VARCHAR(255),
    npi                           VARCHAR(20),
    provider_uuid                 VARCHAR(50),
    address1                      VARCHAR(255),
    address2                      VARCHAR(255),
    city                          VARCHAR(100),
    state                         VARCHAR(10),
    zip9                          VARCHAR(20),
    enrollment_type               VARCHAR(10)  NOT NULL,
    enrollment_eligibility_status VARCHAR(10),
    effective_date                DATE,
    initial_enrollment_date       DATE,
    last_updated_utc_date         TIMESTAMP WITH TIME ZONE,
    enrollment_status_cd          VARCHAR(10),
    enrollment_status_reason      VARCHAR(255),
    status                        VARCHAR(50)  NOT NULL,
    error_message                 TEXT,
    created_at                    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at                    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_stp_enrollment_batch
        FOREIGN KEY (stp_enrollment_batch_id)
            REFERENCES virtual_card.stp_enrollment_batch (id)
);

CREATE INDEX IF NOT EXISTS idx_stp_enrollment_batch_status
    ON virtual_card.stp_enrollment_batch(status);

CREATE INDEX IF NOT EXISTS idx_stp_enrollment_record_batch_id
    ON virtual_card.stp_enrollment_record(stp_enrollment_batch_id);

CREATE UNIQUE INDEX IF NOT EXISTS uk_stp_enrollment_record_tin_type
    ON virtual_card.stp_enrollment_record(provider_tin, enrollment_type);
