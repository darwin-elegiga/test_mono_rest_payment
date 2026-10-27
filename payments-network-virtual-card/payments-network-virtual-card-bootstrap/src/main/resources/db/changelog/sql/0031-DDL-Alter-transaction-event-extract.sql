ALTER TABLE virtual_card.transaction_event_extract ADD COLUMN value float4;

ALTER TABLE virtual_card.transaction_event_extract ALTER COLUMN ifi_name drop not null;

ALTER TABLE virtual_card.transaction_event_extract ALTER COLUMN decline_reason TYPE TEXT;

ALTER TABLE virtual_card.transaction_event_extract ADD COLUMN decline_error_code varchar(250);

ALTER TABLE virtual_card.transaction_event_extract ADD COLUMN transaction_status varchar(50);

ALTER TABLE virtual_card.transaction_event_extract ADD COLUMN transaction_type varchar(50);

ALTER TABLE virtual_card.transaction_event_extract ADD COLUMN auth_type varchar(50);
