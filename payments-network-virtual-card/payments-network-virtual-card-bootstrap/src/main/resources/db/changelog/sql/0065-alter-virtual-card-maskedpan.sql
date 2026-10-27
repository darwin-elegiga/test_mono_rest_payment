ALTER table virtual_card.virtual_cards add
column masked_pan VARCHAR(19);

ALTER table virtual_card.virtual_card_history add
column masked_pan VARCHAR(19);
