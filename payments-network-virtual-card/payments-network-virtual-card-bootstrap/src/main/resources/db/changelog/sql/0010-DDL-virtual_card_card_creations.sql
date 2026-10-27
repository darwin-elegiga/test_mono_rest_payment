CREATE TABLE virtual_card.card_creations (
    id UUID NOT NULL,
    payment_id VARCHAR(32) NOT NULL,
    bin INTEGER NOT NULL,
    amount BIGINT NOT NULL,
    expiry_year VARCHAR(4) NOT NULL,
    expiry_month VARCHAR(2) NOT NULL,
    expiry_date DATE NOT NULL,
    cardholder_id int8 NOT NULL,
    status VARCHAR(32) NOT NULL,
    spend_rule_id int8,
    retry_count INTEGER DEFAULT 0,
    error_code VARCHAR(50),
    error_message VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by varchar(40),
    updated_by varchar(40),
    completed_at TIMESTAMP,
    completed_by VARCHAR(40),
    CONSTRAINT card_creations_pkey PRIMARY KEY (id),
    CONSTRAINT uq_card_creations_payment_id UNIQUE (payment_id),
    CONSTRAINT fk_card_creations_cardholder FOREIGN KEY (cardholder_id)
        REFERENCES virtual_card.cardholders(id),
    CONSTRAINT fk_card_creations_payment_request FOREIGN KEY (payment_id)
        REFERENCES virtual_card.payment_request(payment_details_id),
    CONSTRAINT chk_card_creations_status CHECK (status IN (
        'REQUEST_RECEIVED',
        'REQUEST_INITIATED',
        'REQUEST_ACCEPTED',
        'REQUEST_REJECTED',
        'REQUEST_PROCESSING_FAILED',
        'CALLBACK_PROCESSING_FAILED',
        'CARD_ISSUED',
        'CARD_ISSUE_FAILED',
        'EXCEPTION_OCCURRED'
    )),
    CONSTRAINT fk_card_creations_spend_rule FOREIGN KEY (spend_rule_id)
        REFERENCES virtual_card.spend_rules(id)
);
