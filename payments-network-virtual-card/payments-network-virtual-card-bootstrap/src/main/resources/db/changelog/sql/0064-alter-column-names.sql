
ALTER TABLE virtual_card.account
RENAME COLUMN payer835id TO payer835_id;

ALTER TABLE virtual_card.sub_accounts
RENAME COLUMN payer835id TO payer835_id;

ALTER TABLE virtual_card.payment_request
RENAME COLUMN payr835id TO payr835_id;

ALTER TABLE virtual_card.payment_request_history
RENAME COLUMN payr835id TO payr835_id;
