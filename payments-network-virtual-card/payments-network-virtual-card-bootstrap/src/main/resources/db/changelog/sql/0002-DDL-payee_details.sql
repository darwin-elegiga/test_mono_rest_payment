-- virtual_card.payee_details definition
-- Drop table
-- DROP TABLE virtual_card.payee_details;
CREATE TABLE virtual_card.payee_details (
	id int8 NOT NULL,
	created_by_id varchar(40) NULL,
	created_on timestamp(6) NOT NULL,
	updated_by_id varchar(40) NULL,
	updated_on timestamp(6) NOT NULL,
	"name" VARCHAR(100),
	npi VARCHAR(10),
	receiver_id VARCHAR(50),
	tax_id VARCHAR(9),
	"type" varchar(50) NULL,
	address_id int8 NULL,
	CONSTRAINT payee_details_pkey PRIMARY KEY (id),
	CONSTRAINT uk1juh7ihqder973l96bumwx7u UNIQUE (address_id)
);
-- public.payee_details foreign keys
ALTER TABLE virtual_card.payee_details ADD CONSTRAINT fkcic1f1pl9x6h7smfao1sgjpx8 FOREIGN KEY (address_id) REFERENCES virtual_card.address(id);

CREATE SEQUENCE virtual_card.payee_details_id_seq
	INCREMENT BY 50
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
