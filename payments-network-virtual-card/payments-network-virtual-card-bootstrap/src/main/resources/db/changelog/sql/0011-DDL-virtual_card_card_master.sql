CREATE TABLE virtual_card.virtual_cards (
    id UUID NOT NULL,
    card_guid UUID NOT NULL,
    payment_id VARCHAR(32) NOT NULL,
    initial_card_value BIGINT,
    auth_amount BIGINT,
    cleared_amount BIGINT,
    current_balance BIGINT,
    available_balance BIGINT,
    currency CHAR(3),
    expiry_date DATE NOT NULL,
    spend_rules _text,
    status VARCHAR(25) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by varchar(40),
    updated_at TIMESTAMP,
    updated_by varchar(40),
    CONSTRAINT virtual_cards_pkey PRIMARY KEY (id),
    CONSTRAINT uq_virtual_cards_guid UNIQUE (card_guid),
    CONSTRAINT fk_virtual_cards_request FOREIGN KEY (payment_id)
    REFERENCES virtual_card.card_creations(payment_id),
    CONSTRAINT chk_virtual_cards_status CHECK (status IN (
                'CREATED',
                'CANCELLED',
                'RESET'
            ))
);

