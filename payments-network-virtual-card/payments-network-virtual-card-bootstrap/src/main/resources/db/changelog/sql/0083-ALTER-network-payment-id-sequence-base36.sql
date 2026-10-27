ALTER SEQUENCE virtual_card.network_payment_id_seq
	MAXVALUE 101559956668415
	NO CYCLE;

SELECT setval(
	'virtual_card.network_payment_id_seq',
	GREATEST((SELECT last_value FROM virtual_card.network_payment_id_seq), 2821109907456),
	(SELECT CASE WHEN last_value < 2821109907456 THEN false ELSE true END
		FROM virtual_card.network_payment_id_seq)
);

ALTER SEQUENCE virtual_card.network_payment_id_seq
	START WITH 2821109907456;

ALTER SEQUENCE virtual_card.network_payment_id_seq
	MINVALUE 2821109907456
	NO CYCLE;
