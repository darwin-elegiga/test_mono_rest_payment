CREATE TABLE virtual_card.card_resets (
    id UUID NOT NULL,
    card_guid UUID NOT NULL,
    receipt_id TEXT NOT NULL,
    reset_purpose TEXT NOT NULL,
    balance_before BIGINT,
    balance_after BIGINT,
    currency CHAR(3),
    status VARCHAR(32) NOT NULL,
    error_code VARCHAR(50),
    error_message VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by varchar(40),
    updated_at TIMESTAMP,
    updated_by varchar(40),
    completed_at TIMESTAMP,
    completed_by VARCHAR(40),
    CONSTRAINT card_resets_pkey PRIMARY KEY (id),
    CONSTRAINT fk_card_resets_card FOREIGN KEY (card_guid)
        REFERENCES virtual_card.virtual_cards(card_guid),
    CONSTRAINT chk_card_resets_status CHECK (status IN (
        'REQUEST_RECEIVED',
        'REQUEST_INITIATED',
        'REQUEST_ACCEPTED',
        'REQUEST_REJECTED',
        'REQUEST_PROCESSING_FAILED',
        'CALLBACK_PROCESSING_FAILED',
        'CARD_RESET',
        'CARD_RESET_FAILED',
        'EXCEPTION_OCCURRED'
    ))
);
