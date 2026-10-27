-- This file previously had a corrupted/wrong extension and never actually ran, so the
-- live schema has diverged from what this changelog implies. Statements below are
-- guarded (IF EXISTS) so this runs cleanly against that real current state instead of
-- assuming the drops below have a matching object to act on.

-- Removing the single-column uniqueness on payment_id is required: card_creations can
-- now have more than one row per payment_id (one per reprocessing attempt), with the
-- most recent row treated as authoritative.
alter table virtual_card.virtual_cards drop constraint if exists fk_virtual_cards_request;

alter table virtual_card.card_creations drop constraint if exists uq_card_creations_payment_id;

ALTER TABLE virtual_card.card_creations ADD CONSTRAINT uq_card_creations_payment_id_id UNIQUE (payment_id, id);

-- NOT VALID: existing virtual_cards rows have an id that was never synchronized with
-- card_creations.id (a pre-existing bug, fixed alongside this migration in
-- CreateCardWebhookHandler.saveVirtualCardCreate), so none of them can satisfy this
-- constraint. NOT VALID skips checking those historical rows while still enforcing the
-- constraint on every insert/update from this point forward.
ALTER TABLE virtual_card.virtual_cards ADD CONSTRAINT fk_virtual_cards_request
    FOREIGN KEY (payment_id, id) REFERENCES virtual_card.card_creations(payment_id, id) NOT VALID;