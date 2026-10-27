-- (Optional) Populate existing rows if needed
UPDATE virtual_card.processed_webhooks
SET processed_status = 'RECEIVED'
WHERE processed_status IS NULL;

ALTER TABLE virtual_card.processed_webhooks DROP CONSTRAINT processed_webhooks_pk;

ALTER TABLE virtual_card.processed_webhooks ADD CONSTRAINT processed_webhooks_pkey PRIMARY KEY (id, event_type,processed_status);
