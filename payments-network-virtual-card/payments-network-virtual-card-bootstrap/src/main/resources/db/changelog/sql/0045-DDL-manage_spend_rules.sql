CREATE SEQUENCE virtual_card.manage_spend_rules_id_seq
    INCREMENT BY 1
    MINVALUE 1
    MAXVALUE 9223372036854775807
    START 1
    CACHE 1
    NO CYCLE;

CREATE TABLE virtual_card.manage_spend_rules (
    id BIGINT NOT NULL DEFAULT nextval('virtual_card.manage_spend_rules_id_seq'),
    rule_id VARCHAR(255) NOT NULL UNIQUE,
    optum_rule_schema_id VARCHAR(255) NOT NULL,
    rule_type VARCHAR(255) NOT NULL,
    rule_name VARCHAR(255) NOT NULL,
    maximum_amount BIGINT NOT NULL,
    rule_allowed_mcc _text NULL,
    rule_disallowed_mcc _text NULL,
    rule_allowed_merchant_ids _text NULL,
    rule_disallowed_merchant_ids _text NULL,
    risk_score INTEGER,
    created_by varchar(40) NULL,
    created_at timestamp DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp NULL,
    updated_by varchar(40) NULL,
    CONSTRAINT manage_spend_rules_pkey PRIMARY KEY (id)
);
CREATE INDEX idx_manage_spend_rules_rule_type ON virtual_card.manage_spend_rules (rule_type);
CREATE INDEX idx_manage_spend_rules_rule_name ON virtual_card.manage_spend_rules (rule_name);
