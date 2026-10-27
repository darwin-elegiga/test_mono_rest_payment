CREATE SEQUENCE virtual_card.virtual_card_history_id_seq
    INCREMENT BY 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1
    NO CYCLE;

CREATE TABLE virtual_card.virtual_card_history (
  id int8 NOT NULL DEFAULT nextval(
    'virtual_card.virtual_card_history_id_seq'
  ),
  card_guid UUID NOT NULL,
  request_id VARCHAR(32) NOT NULL,
  status varchar(25),
  receipt_id varchar(20) NULL,
  reason TEXT NULL,
  auth_card_value float NULL,
  currency CHAR(3),
  available_balance BIGINT,
  current_balance BIGINT NULL,
  cleared_balance BIGINT NULL,
  auth_balance BIGINT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  created_by VARCHAR(100),
  CONSTRAINT virtual_card_status_history_pkey PRIMARY KEY (id),
  CONSTRAINT fk_status_history_card FOREIGN KEY (card_guid) REFERENCES virtual_card.virtual_cards(card_guid)
);
