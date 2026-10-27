CREATE INDEX IF NOT EXISTS idx_virtual_card_audit_log_payment_id_created_at
    ON virtual_card.virtual_card_audit_log (payment_id, created_at);