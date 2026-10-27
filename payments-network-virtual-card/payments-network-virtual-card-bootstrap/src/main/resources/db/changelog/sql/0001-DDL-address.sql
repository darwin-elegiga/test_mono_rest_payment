-- public.address definition
-- Drop table
-- DROP TABLE virtual_card.address;
CREATE TABLE virtual_card.address (
	id int8 NOT NULL,
	city varchar(50),
	country varchar(10),
	line1 varchar(45),
	line2 varchar(45),
	postal_code varchar(15),
	state varchar(10),
	CONSTRAINT address_pkey PRIMARY KEY (id)
);

CREATE SEQUENCE virtual_card.address_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
