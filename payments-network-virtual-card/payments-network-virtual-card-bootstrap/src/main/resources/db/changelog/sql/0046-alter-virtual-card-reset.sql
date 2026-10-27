ALTER TABLE virtual_card.card_resets
ADD COLUMN IF NOT EXISTS payment_id VARCHAR(32);
