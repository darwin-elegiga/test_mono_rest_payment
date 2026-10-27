-- virtual_card.shipping_details definition
-- Drop table
-- DROP TABLE virtual_card.shipping_details;
CREATE TABLE virtual_card.shipping_details (
	id int8 NOT NULL,
	created_by_id varchar(40) NULL,
	created_on timestamp(6) NOT NULL,
	updated_by_id varchar(40) NULL,
	updated_on timestamp(6) NOT NULL,
	"name" varchar(50) NULL,
	address_id int8 NULL,
	CONSTRAINT shipping_details_pkey PRIMARY KEY (id),
	CONSTRAINT uk2ipsun939og2ee8njsfs63upc UNIQUE (address_id)
);
-- public.shipping_details foreign keys
ALTER TABLE virtual_card.shipping_details ADD CONSTRAINT fk9rswbrm4llpvvqlbaye11uk17 FOREIGN KEY (address_id) REFERENCES virtual_card.address(id);

CREATE SEQUENCE virtual_card.shipping_details_id_seq
	INCREMENT BY 50
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
