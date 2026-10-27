ALTER TABLE virtual_card.sub_accounts
 ADD COLUMN return_address_id int8 NULL;

ALTER TABLE virtual_card.sub_accounts ADD CONSTRAINT sub_accounts_return_address_id FOREIGN KEY (return_address_id) REFERENCES virtual_card.address(id);