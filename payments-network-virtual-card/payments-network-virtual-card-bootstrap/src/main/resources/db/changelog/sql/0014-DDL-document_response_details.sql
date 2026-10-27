
CREATE TABLE virtual_card.document_response_details (
	doc_response_id int8 GENERATED ALWAYS AS IDENTITY( INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 START 1 CACHE 1 NO CYCLE) NOT NULL,
	document_id varchar(255) NULL,
	document_url varchar(255) NULL,
	error_desc varchar(255) NULL,
	doc_type varchar(255) NULL,
	meta_data varchar(255) NULL,
	created_dt timestamp(6) NOT NULL,
	created_by varchar(255) NOT NULL,
	last_changed_dt timestamp(6) NOT NULL,
	last_changed_by varchar(255) NOT NULL,
	network_payment_id varchar(255) NULL,
	CONSTRAINT document_response_details_pkey PRIMARY KEY (doc_response_id),
	CONSTRAINT unique_network_payment_id UNIQUE (network_payment_id)
);