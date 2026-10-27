

CREATE TABLE virtual_card.logo_details (
	id uuid NOT NULL,
	created_by_id varchar(40) NULL,
	created_on timestamp(6) NULL,
	last_update timestamp(6) NULL,
	updated_by_id varchar(40) NULL,
	logo_url varchar(255) NOT NULL,
	"type" varchar(255) NULL,
	org_short_code varchar(255) NULL,
	account_short_code varchar(255) NULL,
	sub_account_short_code varchar(255) NULL,
	CONSTRAINT logo_details_pkey PRIMARY KEY (id)
);