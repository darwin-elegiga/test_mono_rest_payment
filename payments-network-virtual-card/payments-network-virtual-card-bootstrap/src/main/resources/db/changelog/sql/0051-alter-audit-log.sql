ALTER TABLE virtual_card.virtual_card_audit_log
DROP CONSTRAINT IF EXISTS virtual_card_audit_log_status_check;

UPDATE virtual_card.virtual_card_audit_log
SET status = 'AUTH_RESET'
WHERE status = 'AUTH_CLEARED';

UPDATE virtual_card.virtual_card_audit_log
SET status = 'AUTH_RESET_FAILED'
WHERE status = 'AUTH_CLEAR_FAILED';

ALTER TABLE virtual_card.virtual_card_audit_log
ADD CONSTRAINT virtual_card_audit_log_status_check
CHECK (status IN (
    'REQUEST_RECEIVED',
    'REQUEST_INITIATED',
    'REQUEST_ACCEPTED',
    'REQUEST_REJECTED',
    'REQUEST_PROCESSING_FAILED',
    'CALLBACK_PROCESSING_FAILED',
    'CARD_ISSUED',
    'CARD_ISSUE_FAILED',
    'CARD_CANCELLED',
    'CARD_CANCELLATION_FAILED',
    'AUTH_REQUEST_ACCEPTED',
    'AUTH_REQUEST_REJECTED',
    'AUTH_RESET',
    'AUTH_RESET_FAILED',
    'EXCEPTION_OCCURRED',
    'ERROR_OCCURRED'
));