CREATE TABLE virtual_card.company_logo_details (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    logo_url varchar(255) NOT NULL,
    "type" varchar(255) NOT NULL,
    created_by_id VARCHAR(40) NULL,
    created_on TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_by_id VARCHAR(40) NULL,
    last_update TIMESTAMP
);
