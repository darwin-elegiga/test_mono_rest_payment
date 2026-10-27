ALTER TABLE virtual_card.virtual_card_history ALTER COLUMN initial_balance TYPE NUMERIC(19,2);

ALTER TABLE virtual_card.virtual_card_history RENAME COLUMN request_id TO payment_id;