

CREATE TABLE virtual_card.account (
	account_id uuid NOT NULL,
	claim_office_phone_number varchar(256) NULL,
	claim_office_phone_number_extension varchar(256) NULL,
	claim_officeurl varchar(256) NULL,
	contact_email varchar(250) NULL,
	fax_number varchar(50) NULL,
	funding_method varchar(50) NULL,
	hold_and_release bool NULL,
	market_type varchar(50) NULL,
	"name" varchar(250) NULL,
	payer835id varchar(9) NULL,
	phone_number varchar(50) NULL,
	program_id varchar(256) NULL,
	technical_contact_name varchar(60) NULL,
	technical_email_address varchar(256) NULL,
	technical_fax_number varchar(256) NULL,
	technical_phone_number varchar(256) NULL,
	technical_phone_number_extension varchar(256) NULL,
	technicalurl varchar(256) NULL,
	tin varchar(9) NOT NULL,
	address_id int8 NULL,
	"type" varchar(50) NULL,
	account_short_code varchar(20) NULL,
	has_custom_logo bool NULL,
	org_short_code varchar(20) NULL,
	contact_name varchar(100) NULL,
	onboarding_status varchar(20) NULL,
	created_by_id varchar(40) NULL,
    created_on timestamp(6) NULL,
    updated_by_id varchar(40) NULL,
    updated_on timestamp(6) NULL,
	CONSTRAINT payer_pkey PRIMARY KEY (account_id)
	--CONSTRAINT uk5b9wrhrtvr16o401gsskocl9n UNIQUE (address_id)
);


--ALTER TABLE virtual_card.account ADD CONSTRAINT fkq5bpqtopghb64491sv0im4qgs FOREIGN KEY (address_id) REFERENCES virtual_card.address(id);