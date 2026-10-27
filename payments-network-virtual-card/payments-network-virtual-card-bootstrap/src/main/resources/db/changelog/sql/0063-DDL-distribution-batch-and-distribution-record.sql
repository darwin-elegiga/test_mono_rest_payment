CREATE TABLE virtual_card.distribution_batch (
    id             BIGSERIAL    PRIMARY KEY,
    correlation_id VARCHAR(100) NOT NULL UNIQUE,
    zip_file_name  VARCHAR(255),
    status         VARCHAR(50)  NOT NULL,
    retry_count    INT          DEFAULT 0,
    error_message  TEXT,
    created_at     TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at     TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    sent_at        TIMESTAMP,
    acknowledge_at TIMESTAMP
);

CREATE TABLE virtual_card.distribution_record (
    id                    BIGSERIAL    PRIMARY KEY,
    distribution_batch_id BIGINT       NOT NULL,
    payment_id            VARCHAR(100) NOT NULL,
    card_guid             VARCHAR(100) NOT NULL,
    html_file_name        VARCHAR(255),
    distribution_method   VARCHAR(20),
    status                VARCHAR(50),
    error_message         TEXT,
    created_at            TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at            TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_batch FOREIGN KEY(distribution_batch_id)
        REFERENCES virtual_card.distribution_batch(id)
);

CREATE INDEX idx_record_batch    ON virtual_card.distribution_record(distribution_batch_id);
CREATE INDEX idx_record_status   ON virtual_card.distribution_record(distribution_batch_id, status);
CREATE INDEX idx_batch_status    ON virtual_card.distribution_batch(status);
