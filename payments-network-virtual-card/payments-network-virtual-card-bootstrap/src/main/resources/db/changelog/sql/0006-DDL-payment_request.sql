-- virtual_card.payment_request definition
-- Drop table
-- DROP TABLE virtual_card.payment_request;
CREATE TABLE virtual_card.payment_request (
	id int8 NOT NULL,
	clm_url varchar(500),
	documents varchar(500) NULL,
	doc_type varchar(6) NULL,
	origin varchar(50) NOT NULL,
	payr835id varchar(36) NULL,
	correlation_id VARCHAR(32) NULL,
	status varchar(4) NOT NULL,
	submitter_id VARCHAR(50) NOT NULL,
	total_clm_count int4 NOT NULL,
	payee_details_id int8 NULL,
	payment_details_id varchar(32) NULL,
	shipping_details_id int8 NULL,
	org_short_code varchar(20) NULL,
    account_short_code varchar(20) NULL,
    sub_account_short_code varchar(20) NULL,
    plb boolean default false,
	metadata varchar(500) NULL,
	st_se_segment text NULL,
	created_by_id varchar(40) NULL,
    created_on timestamp(6) NOT NULL,
    updated_by_id varchar(40) NULL,
    updated_on timestamp(6) NOT NULL,
	CONSTRAINT payment_request_pkey PRIMARY KEY (id),
	CONSTRAINT ukafra3hr4vairto85o820vv5io UNIQUE (shipping_details_id),
	CONSTRAINT ukav1aojucetlo5c5lx0s35y6n8 UNIQUE (payment_details_id),
	CONSTRAINT ukkwnimbk8c9m1dcbds12r7j4mn UNIQUE (payee_details_id)
);
-- virtual_card.payment_request foreign keys
ALTER TABLE virtual_card.payment_request ADD CONSTRAINT fk5aknmlsmp4y06hq5ayen5mbmh FOREIGN KEY (shipping_details_id) REFERENCES virtual_card.shipping_details(id);
ALTER TABLE virtual_card.payment_request ADD CONSTRAINT fk9at9k9dpwhmn2057jirbst3lo FOREIGN KEY (payee_details_id) REFERENCES virtual_card.payee_details(id);
ALTER TABLE virtual_card.payment_request ADD CONSTRAINT fkl8v4fqyeqbstm7iwpqubq4iyi FOREIGN KEY (payment_details_id) REFERENCES virtual_card.payment_details(id);

create sequence virtual_card.payment_request_id_seq start with 1 increment by 50;