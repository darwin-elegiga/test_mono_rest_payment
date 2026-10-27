CREATE SEQUENCE virtual_card.billing_addresses_id_seq
    INCREMENT BY 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1
    NO CYCLE;

CREATE SEQUENCE virtual_card.cardholders_id_seq
    INCREMENT BY 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1
    NO CYCLE;

CREATE TABLE virtual_card.billing_addresses (
	id int8 NOT NULL DEFAULT nextval('virtual_card.billing_addresses_id_seq'),
    line1 VARCHAR(250) NOT NULL,
    city VARCHAR(50) NOT NULL,
    country VARCHAR(3) NOT NULL,
    postal_code VARCHAR(20) NOT NULL,
    CONSTRAINT billing_addresses_pkey PRIMARY KEY (id)
);

CREATE TABLE virtual_card.cardholders (
	id int8 NOT NULL DEFAULT nextval('virtual_card.cardholders_id_seq'),
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    email VARCHAR(255) NULL,
    phone_number VARCHAR(20),
    billing_address_id int8,
    CONSTRAINT cardholders_pkey PRIMARY KEY (id),
    CONSTRAINT fk_cardholders_billing_address FOREIGN KEY (billing_address_id) REFERENCES virtual_card.billing_addresses(id)
);
