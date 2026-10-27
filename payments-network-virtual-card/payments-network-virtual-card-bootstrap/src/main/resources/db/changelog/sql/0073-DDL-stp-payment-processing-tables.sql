CREATE TABLE IF NOT EXISTS virtual_card.stp_payment_processing (
    id BIGSERIAL PRIMARY KEY,
    payment_request_id BIGINT NOT NULL
        REFERENCES virtual_card.payment_request(id),
    network_partner VARCHAR(100) NOT NULL,
    network_payment_id VARCHAR(50) NOT NULL,
    status VARCHAR(50) NOT NULL CHECK (
        status IN (
            'STP_PROCESSING_FAILED',
            'REMITTANCE_SENT',
            'STP_CONFIRMED',
            'STP_REJECTED'
        )
    ),
    retry_count INTEGER NOT NULL DEFAULT 0,
    rejection_code VARCHAR(20),
    rejection_reason VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE virtual_card.stp_payment_processing
ADD CONSTRAINT uq_stp_payment_processing_network_payment_id
UNIQUE (network_payment_id);

CREATE TABLE IF NOT EXISTS virtual_card.stp_payment_processing_history (
    id BIGSERIAL PRIMARY KEY,
    network_payment_id VARCHAR(50) NOT NULL
        REFERENCES virtual_card.stp_payment_processing(network_payment_id),
    status VARCHAR(50) NOT NULL CHECK (
        status IN (
            'STP_PROCESSING_FAILED',
            'REMITTANCE_SENT',
            'STP_CONFIRMED',
            'STP_REJECTED'
        )
    ),
    remarks VARCHAR(1000),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_stp_payment_processing_event_network_payment_id
    ON virtual_card.stp_payment_processing_history(network_payment_id);

