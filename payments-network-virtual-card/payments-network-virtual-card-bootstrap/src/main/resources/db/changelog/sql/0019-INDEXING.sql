CREATE INDEX idx_request_id ON virtual_card.card_creations (id);

CREATE INDEX idx_network_payment_id ON virtual_card.document_response_details (network_payment_id);

CREATE INDEX idx_card_cancellations_request_id ON virtual_card.card_cancellations (id);

CREATE INDEX idx_card_resets_card_guid ON virtual_card.card_resets (card_guid);

CREATE INDEX idx_virtual_card_request_id ON virtual_card.virtual_cards (payment_id);

CREATE INDEX idx_virtual_card_card_guid ON virtual_card.virtual_cards (card_guid);

CREATE INDEX idx_card_resets_receipt_id ON virtual_card.card_resets (receipt_id);
