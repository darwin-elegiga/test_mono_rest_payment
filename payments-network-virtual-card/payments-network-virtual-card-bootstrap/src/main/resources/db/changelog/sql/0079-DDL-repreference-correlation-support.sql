ALTER TABLE virtual_card.payment_request
    ADD COLUMN IF NOT EXISTS re_preference_flag BOOLEAN DEFAULT FALSE NOT NULL;

ALTER TABLE virtual_card.payment_request
    ADD COLUMN IF NOT EXISTS re_preference_processed BOOLEAN DEFAULT FALSE NOT NULL;

ALTER TABLE virtual_card.payment_request_history
    ADD COLUMN IF NOT EXISTS re_preference_flag BOOLEAN DEFAULT FALSE NOT NULL;

CREATE TABLE IF NOT EXISTS virtual_card.correlation_mapping (
    actual_correlation_id VARCHAR(32) NOT NULL,
    internal_correlation_id VARCHAR(32) PRIMARY KEY,
    created_on TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_correlation_mapping_not_self CHECK (actual_correlation_id <> internal_correlation_id)
);
