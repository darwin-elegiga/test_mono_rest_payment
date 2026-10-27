CREATE TABLE virtual_card.processed_webhooks (
    id VARCHAR(40) PRIMARY KEY,
    event_type VARCHAR(60) NOT NULL,
    processed_at timestamp,
    processed_status VARCHAR(20) NOT NULL
);
CREATE INDEX idx_processed_webhooks_id ON virtual_card.processed_webhooks(id);
CREATE INDEX idx_processed_webhooks_id_event_type ON virtual_card.processed_webhooks(id, event_type);