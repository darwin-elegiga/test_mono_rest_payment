ALTER TABLE virtual_card.card_resets
DROP CONSTRAINT IF EXISTS chk_card_resets_status;

UPDATE virtual_card.card_resets
SET status = 'AUTH_RESET'
WHERE status = 'AUTH_CLEARED';

UPDATE virtual_card.card_resets
SET status = 'AUTH_RESET_FAILED'
WHERE status = 'AUTH_CLEAR_FAILED';

ALTER TABLE virtual_card.card_resets
ADD CONSTRAINT chk_card_resets_status
CHECK (status IN (
    'REQUEST_RECEIVED',
    'REQUEST_INITIATED',
    'REQUEST_ACCEPTED',
    'REQUEST_REJECTED',
    'REQUEST_PROCESSING_FAILED',
    'CALLBACK_PROCESSING_FAILED',
    'AUTH_RESET',
    'AUTH_RESET_FAILED',
    'EXCEPTION_OCCURRED'
));