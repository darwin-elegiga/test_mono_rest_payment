CREATE SCHEMA IF NOT EXISTS virtual_card;

CREATE TABLE virtual_card.organizations (
	org_id uuid NOT NULL,
	market_type varchar(50) NULL,
	org_status varchar(6) NULL,
	"name" varchar(250) NULL,
	address_id int8 NULL,
	fax_number varchar(50) NULL,
	phone_number varchar(50) NULL,
	contact_email varchar(250) NULL,
	tin varchar(9) NULL,
	onboarded_on_dt timestamp(6) NULL,
	program_id varchar(256) NULL,
	funding_method varchar(50) NULL,
	has_custom_logo bool NULL,
	claim_office_phone_number varchar(256) NULL,
	claim_office_phone_number_extension varchar(256) NULL,
	claim_office_url varchar(256) NULL,
	technical_contact_name varchar(60) NULL,
	technical_phone_number varchar(256) NULL,
	technical_phone_number_extension varchar(256) NULL,
	technical_url varchar(256) NULL,
	technical_fax_number varchar(256) NULL,
	technical_email_address varchar(256) NULL,
	onboarding_status varchar(20) NULL,
	created_on timestamp(6) NULL,
	updated_on timestamp(6) NULL,
	created_by varchar(50) NULL,
	updated_by varchar(50) NULL,
	"type" varchar(50) NULL,
	org_short_code varchar(20) NULL,
	contact_name varchar(100) NULL,
	--CONSTRAINT organizations_address_id_key UNIQUE (address_id),
	CONSTRAINT organizations_status_check CHECK (((org_status)::text = ANY (ARRAY[('ACTIVE'::character varying)::text, ('CLOSED'::character varying)::text]))),
	CONSTRAINT organizations_pkey PRIMARY KEY (org_id)
	--CONSTRAINT organizations_address_id FOREIGN KEY (address_id) REFERENCES virtual_card.address(id)
);

CREATE TABLE virtual_card.bank_accounts (
	bank_account_id uuid NOT NULL,
	created_by varchar(40) NULL,
	created_on timestamp(6) NULL,
	updated_by varchar(40) NULL,
	updated_on timestamp(6) NULL,
	account_number_last_four_digits varchar(10) NOT NULL,
	account_type varchar(50) NOT NULL,
	account_name varchar(50) NULL,
	routing_number varchar(100) NOT NULL,
	org_id uuid NULL,
	"type" varchar(50) NULL,
	verification_status varchar(50) NOT NULL,
	account_number varchar(256) NULL,
	encryption_at_rest_version varchar(50) NULL,
	org_short_code varchar(100) NULL,
	display_name varchar(256) NULL,
	CONSTRAINT bank_account_details_pkey PRIMARY KEY (bank_account_id)
);

CREATE TABLE virtual_card.local_bank_accounts (
	local_bank_account_id uuid NOT NULL,
	account_name varchar(256) NULL,
	routing_number varchar(9) NOT NULL,
	account_number varchar(256) NOT NULL,
	account_number_last_four_digits varchar(4) NOT NULL,
	account_type varchar(50) NOT NULL,
	verification_status varchar(50) NOT NULL,
	ach_company_id varchar(10) NULL,
	created_on timestamp(6) NULL,
	updated_on timestamp(6) NULL,
	"type" varchar(50) NULL,
	encryption_at_rest_version varchar(50) NULL,
	CONSTRAINT local_bank_account_details_pkey PRIMARY KEY (local_bank_account_id)
);