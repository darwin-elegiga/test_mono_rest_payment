-- virtual_card.payment_details definition
-- Drop table
-- DROP TABLE virtual_card.payment_details;
CREATE TABLE virtual_card.payment_details (
	id varchar(32) NOT NULL,
	created_by_id varchar(40) NULL,
	created_on timestamp(6) NOT NULL,
	updated_by_id varchar(40) NULL,
	updated_on timestamp(6) NOT NULL,
	adjudication_cutoff_date date NULL,
	adjudication_version VARCHAR(50) NULL,
	amount float4 NOT NULL,
	issuer_payment_id VARCHAR(50) NULL,
	jurisdictional_body VARCHAR(255) NULL,
	number_of_attached_pdfs int4 NULL,
	payment_date date NOT NULL,
	reference_id varchar(16) NULL,
	remit_method varchar(4) NOT NULL,
	settlement_time VARCHAR(6) NULL,
	network_payment_id varchar(50) NULL,
	type VARCHAR(50) NULL,
	CONSTRAINT payment_details_pkey PRIMARY KEY (id),
	CONSTRAINT payment_details_remit_method_check CHECK (((remit_method)::text = ANY ((ARRAY['PDF'::character varying, 'TEXT'::character varying, 'DATA'::character varying, 'NONE'::character varying])::text[])))
);