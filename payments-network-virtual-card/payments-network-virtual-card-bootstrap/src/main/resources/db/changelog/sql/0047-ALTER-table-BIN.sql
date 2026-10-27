-- Synchronize sequence after inserts
SELECT setval('virtual_card.bin_id_seq', (SELECT MAX(id) FROM virtual_card.bin));

UPDATE virtual_card.bin SET bin = '111111' WHERE bin = '123456';

INSERT INTO virtual_card.bin
(bin, funding_type, created_at, updated_at, created_by, updated_by)
VALUES(111111, 'PRE_FUNDED_NEXT_DAY', now(), null, 'system', null);

INSERT INTO virtual_card.bin
(bin, funding_type, created_at, updated_at, created_by, updated_by)
VALUES(111111, 'PRE_FUNDED_SAME_DAY', now(), null, 'system', null);

ALTER TABLE virtual_card.bank_accounts ADD COLUMN key_name varchar(256) NULL;
