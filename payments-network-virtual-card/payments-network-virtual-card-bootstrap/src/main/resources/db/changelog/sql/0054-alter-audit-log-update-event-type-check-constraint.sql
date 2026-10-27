ALTER TABLE virtual_card.virtual_card_audit_log
DROP CONSTRAINT virtual_card_audit_log_event_type_check;

ALTER TABLE virtual_card.virtual_card_audit_log
ADD CONSTRAINT virtual_card_audit_log_event_type_check
CHECK (event_type IN ('CREATE', 'CANCEL', 'RESET', 'AUTH', 'CLEARING'));