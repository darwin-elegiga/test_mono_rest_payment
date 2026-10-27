CREATE SEQUENCE virtual_card.bin_id_seq
    INCREMENT BY 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1
    NO CYCLE;


CREATE SEQUENCE virtual_card.spend_rules_id_seq
    INCREMENT BY 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1
    NO CYCLE;

CREATE TABLE virtual_card.bin (
    id int8 NOT NULL DEFAULT nextval('virtual_card.bin_id_seq'),
    bin INTEGER NOT NULL,
    funding_type VARCHAR(50),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    created_by varchar(40),
    updated_by varchar(40),
    CONSTRAINT bin_pkey PRIMARY KEY (id)
);

CREATE TABLE virtual_card.spend_rules (
    id BIGINT NOT NULL DEFAULT nextval('virtual_card.spend_rules_id_seq'),
    rule_code _text NOT NULL,
   	org_short_code varchar(20) NOT NULL,
   	account_short_code varchar(20) NULL,
   	sub_account_short_code varchar(20) NULL,
   	created_at timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
   	updated_at timestamp NULL,
   	created_by varchar(40) NULL,
   	updated_by varchar(40) NULL,
   	CONSTRAINT spend_rules_pkey PRIMARY KEY (id)
);


