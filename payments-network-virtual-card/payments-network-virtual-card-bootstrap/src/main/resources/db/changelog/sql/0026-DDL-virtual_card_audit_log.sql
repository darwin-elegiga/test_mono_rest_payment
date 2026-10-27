CREATE SEQUENCE virtual_card.virtual_cards_audit_log_id_seq
    INCREMENT BY 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1
    NO CYCLE;

CREATE TABLE virtual_card.virtual_card_audit_log (
    id int8 NOT NULL DEFAULT nextval('virtual_card.virtual_cards_audit_log_id_seq'),
    payment_id VARCHAR(32) NOT NULL,
    card_guid UUID,
    request_id UUID,
    receipt_id varchar(32),
    event_type VARCHAR(16) NOT NULL CHECK (event_type IN ('CREATE', 'CANCEL', 'RESET', 'AUTH')),
    status VARCHAR(32) NOT NULL CHECK (status IN (
        'REQUEST_RECEIVED',
        'REQUEST_INITIATED',
        'REQUEST_ACCEPTED',
        'REQUEST_REJECTED',
        'REQUEST_PROCESSING_FAILED',
        'CALLBACK_PROCESSING_FAILED',
        'CARD_ISSUED',
        'CARD_ISSUE_FAILED',
        'CARD_CANCELLED',
        'CARD_CANCELLATION_FAILED',
        'CARD_RESET',
        'CARD_RESET_FAILED',
        'EXCEPTION_OCCURRED'
    )),
    source VARCHAR(64) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(64)
);
