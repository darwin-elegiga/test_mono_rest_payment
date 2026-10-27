ALTER TABLE virtual_card.card_details ADD COLUMN zeta_setup_keys_version varchar(256) NULL;

ALTER TABLE virtual_card.clear_extract ADD COLUMN record_status varchar(50) NULL;
ALTER TABLE virtual_card.auth_extract ADD COLUMN clearing_status varchar(50) NULL;

