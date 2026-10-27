CREATE INDEX IF NOT EXISTS idx_clear_extract_watermark
    ON virtual_card.clear_extract ((COALESCE(updated_at, created_at)));
