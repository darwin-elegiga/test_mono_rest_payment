INSERT INTO virtual_card.billing_addresses
(id, line1, city, country, postal_code)
VALUES(1, '10 Downing St', 'London', 'GB', 'SW1A 1AA');

INSERT INTO virtual_card.cardholders
(id, first_name, last_name, email, phone_number, billing_address_id)
VALUES(1, 'UHG', 'Healthcare', 'payables@uhg.com', '999999999999', 1);

INSERT INTO virtual_card.bin
(id, bin, funding_type, created_at, updated_at, created_by, updated_by)
VALUES(1, 123456, 'POST_FUNDED', '2025-07-29 17:01:34.031', NULL, 'system', NULL);
