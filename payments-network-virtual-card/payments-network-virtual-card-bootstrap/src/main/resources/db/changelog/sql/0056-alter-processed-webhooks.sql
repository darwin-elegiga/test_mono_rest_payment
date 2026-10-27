ALTER TABLE virtual_card.processed_webhooks DROP CONSTRAINT processed_webhooks_pkey;
ALTER TABLE virtual_card.processed_webhooks ADD CONSTRAINT processed_webhooks_pk PRIMARY KEY (id, event_type);

-- Ensure processed_at is always present
ALTER TABLE virtual_card.processed_webhooks
  ALTER COLUMN processed_at SET DEFAULT now(),
  ALTER COLUMN processed_at SET NOT NULL;


-- Index to accelerate time-based cleanup (TTL)
CREATE INDEX IF NOT EXISTS idx_processed_webhooks_processed_at
  ON virtual_card.processed_webhooks (processed_at);

