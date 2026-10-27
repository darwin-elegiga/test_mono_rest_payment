CREATE TABLE virtual_card.card_cancellations (
    id UUID NOT NULL,
    payment_id VARCHAR(32) NOT NULL,
    card_guid UUID NOT NULL,
    reason_code TEXT NOT NULL,
    status VARCHAR(32) NOT NULL,
    error_code VARCHAR(50),
    error_message VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by varchar(40),
    updated_at TIMESTAMP,
    updated_by varchar(40),
    completed_at TIMESTAMP,
    completed_by VARCHAR(40),
    CONSTRAINT card_cancellations_pkey PRIMARY KEY (id),
    CONSTRAINT fk_card_cancellations_card FOREIGN KEY (card_guid)
        REFERENCES virtual_card.virtual_cards(card_guid),
    CONSTRAINT chk_card_cancellations_status CHECK (status IN (
        'REQUEST_RECEIVED',
        'REQUEST_INITIATED',
        'REQUEST_ACCEPTED',
        'REQUEST_REJECTED',
        'REQUEST_PROCESSING_FAILED',
        'CALLBACK_PROCESSING_FAILED',
        'CARD_CANCELLED',
        'CARD_CANCELLATION_FAILED',
        'EXCEPTION_OCCURRED'
    ))
);
