ALTER TABLE virtual_card.payment_request ALTER COLUMN clm_url TYPE varchar(1000) USING clm_url::varchar(1000);
ALTER TABLE virtual_card.payment_request ALTER COLUMN documents TYPE varchar(1000) USING documents::varchar(1000);
ALTER TABLE virtual_card.payment_request ALTER COLUMN metadata TYPE varchar(1000) USING metadata::varchar(1000);

ALTER TABLE virtual_card.payment_request_history ALTER COLUMN clm_url TYPE varchar(1000) USING clm_url::varchar(1000);
ALTER TABLE virtual_card.payment_request_history ALTER COLUMN documents TYPE varchar(1000) USING documents::varchar(1000);
ALTER TABLE virtual_card.payment_request_history ALTER COLUMN metadata TYPE varchar(1000) USING metadata::varchar(1000);
