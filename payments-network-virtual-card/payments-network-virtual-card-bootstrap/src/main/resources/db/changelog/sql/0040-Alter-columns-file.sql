ALTER TABLE virtual_card.card_creations DROP CONSTRAINT fk_card_creations_spend_rule;
ALTER TABLE virtual_card.card_creations ALTER COLUMN error_message TYPE TEXT;

ALTER TABLE virtual_card.card_resets
    DROP COLUMN balance_before,
    DROP COLUMN balance_after,
    DROP COLUMN currency;

ALTER TABLE virtual_card.virtual_cards DROP COLUMN auth_amount;
ALTER TABLE virtual_card.virtual_cards DROP COLUMN cleared_amount;
ALTER TABLE virtual_card.virtual_cards RENAME COLUMN initial_card_value TO initial_balance;

ALTER TABLE virtual_card.virtual_card_history DROP COLUMN auth_balance;
ALTER TABLE virtual_card.virtual_card_history DROP COLUMN cleared_balance;
ALTER TABLE virtual_card.virtual_card_history DROP COLUMN auth_card_value;

ALTER TABLE virtual_card.virtual_card_history  ADD COLUMN initial_balance BIGINT;

