-- Add error message capture for payment processing failures (API/queue/worker/callback)
ALTER TABLE virtual_card.payment_request ADD COLUMN error_message text NULL;
ALTER TABLE virtual_card.payment_request_history ADD COLUMN error_message text NULL;
