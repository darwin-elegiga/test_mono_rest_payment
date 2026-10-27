CREATE INDEX IF NOT EXISTS idx_clear_extract_receipt_id
    ON virtual_card.clear_extract (receipt_id);
