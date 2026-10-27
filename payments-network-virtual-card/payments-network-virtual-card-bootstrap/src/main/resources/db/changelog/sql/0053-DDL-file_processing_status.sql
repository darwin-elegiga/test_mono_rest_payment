CREATE SEQUENCE virtual_card.file_processing_status_id_seq
    INCREMENT BY 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1
    NO CYCLE;

CREATE TABLE virtual_card.file_processing_status (
    id BIGINT NOT NULL DEFAULT nextval('virtual_card.file_processing_status_id_seq'),
    file_name VARCHAR(255) NULL,
    file_type VARCHAR(50) NOT NULL CHECK (file_type IN ('CLEAR', 'AUTH', 'INVALID_CARD_AUTH_DECLINE')),
    status VARCHAR(50) NOT NULL CHECK (status IN ('SUCCESS', 'FAILED', 'PARTIAL_SUCCESS')),
    record_count INT,
    error_message TEXT,
    processed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_by VARCHAR(100) NOT NULL,
    record_errors JSONB,
    CONSTRAINT file_processing_status_pkey PRIMARY KEY (id)
);
