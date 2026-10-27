CREATE TABLE virtual_card.orphan_events (
    id BIGINT NOT NULL,
    card_id UUID NULL,
    receipt_id varchar(20) NULL,
    original_auth_request_id varchar(256) NULL,
    client_request_id VARCHAR(32) NULL,
    payment_id VARCHAR(32) NULL,
    event_type VARCHAR(20),
    event_payload JSONB, -- raw webhook data
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(40),
    updated_at TIMESTAMP,
    updated_by VARCHAR(40),
    CONSTRAINT orphan_events_pkey PRIMARY KEY (id)
);

CREATE SEQUENCE virtual_card.orphan_events_id_seq
    INCREMENT BY 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1
    NO CYCLE;

CREATE INDEX idx_orphan_events_card_id ON virtual_card.orphan_events (card_id);
CREATE INDEX idx_orphan_events_payment_id ON virtual_card.orphan_events (payment_id);
CREATE INDEX idx_orphan_events_client_request_id ON virtual_card.orphan_events (client_request_id);
CREATE INDEX idx_orphan_events_receipt_id ON virtual_card.orphan_events (receipt_id);
CREATE INDEX idx_orphan_events_original_auth_request_id ON virtual_card.orphan_events (original_auth_request_id);