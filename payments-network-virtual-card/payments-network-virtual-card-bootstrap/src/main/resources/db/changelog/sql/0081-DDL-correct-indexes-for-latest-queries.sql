
CREATE INDEX IF NOT EXISTS ix_payment_request_repreference_pending
    ON virtual_card.payment_request (org_short_code, correlation_id, id)
    WHERE re_preference_flag = TRUE
      AND re_preference_processed = FALSE
      AND status = 'PI';
