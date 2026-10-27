ALTER TABLE virtual_card.card_resets
DROP CONSTRAINT IF EXISTS chk_card_resets_status;

UPDATE virtual_card.card_resets
SET status = 'AUTH_CLEARED'
WHERE status = 'CARD_RESET';

UPDATE virtual_card.card_resets
SET status = 'AUTH_CLEAR_FAILED'
WHERE status = 'CARD_RESET_FAILED';

ALTER TABLE virtual_card.card_resets
ADD CONSTRAINT chk_card_resets_status
CHECK (status IN (
    'REQUEST_RECEIVED',
    'REQUEST_INITIATED',
    'REQUEST_ACCEPTED',
    'REQUEST_REJECTED',
    'REQUEST_PROCESSING_FAILED',
    'CALLBACK_PROCESSING_FAILED',
    'AUTH_CLEARED',
    'AUTH_CLEAR_FAILED',
    'EXCEPTION_OCCURRED'
));