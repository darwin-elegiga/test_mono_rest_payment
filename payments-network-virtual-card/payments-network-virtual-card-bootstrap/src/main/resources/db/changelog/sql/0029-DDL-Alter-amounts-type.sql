ALTER TABLE virtual_card.virtual_cards  ALTER COLUMN initial_card_value TYPE NUMERIC(19,2);
ALTER TABLE virtual_card.virtual_cards ALTER COLUMN auth_amount TYPE NUMERIC(19,2);
ALTER TABLE virtual_card.virtual_cards ALTER COLUMN cleared_amount TYPE NUMERIC(19,2);
ALTER TABLE virtual_card.virtual_cards ALTER COLUMN current_balance TYPE NUMERIC(19,2);
ALTER TABLE virtual_card.virtual_cards ALTER COLUMN available_balance TYPE NUMERIC(19,2);

ALTER TABLE virtual_card.virtual_card_history ALTER COLUMN auth_balance TYPE NUMERIC(19,2);
ALTER TABLE virtual_card.virtual_card_history ALTER COLUMN cleared_balance TYPE NUMERIC(19,2);
ALTER TABLE virtual_card.virtual_card_history ALTER COLUMN current_balance TYPE NUMERIC(19,2);
ALTER TABLE virtual_card.virtual_card_history ALTER COLUMN available_balance TYPE NUMERIC(19,2);

ALTER TABLE virtual_card.payment_details ALTER COLUMN amount TYPE NUMERIC(19,2);
ALTER TABLE virtual_card.payment_request_history ALTER COLUMN amount TYPE NUMERIC(19,2);
