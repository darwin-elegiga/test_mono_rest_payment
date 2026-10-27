-- 1) Add NOT NULL on or_short_code,account_short_code and subaccount_short_code
ALTER TABLE virtual_card.organizations
  ALTER COLUMN org_short_code SET NOT NULL;

ALTER TABLE virtual_card.account
  ALTER COLUMN org_short_code SET NOT NULL,
  ALTER COLUMN account_short_code SET NOT NULL;

ALTER TABLE virtual_card.sub_accounts
  ALTER COLUMN org_short_code SET NOT NULL,
  ALTER COLUMN account_short_code SET NOT NULL,
  ALTER COLUMN sub_account_short_code SET NOT NULL;


-- 2) Add UNIQUE constraints
ALTER TABLE virtual_card.organizations
  ADD CONSTRAINT ux_org_org_short_code UNIQUE (org_short_code);

ALTER TABLE virtual_card.account
  ADD CONSTRAINT ux_account_org_acct_short
  UNIQUE (org_short_code, account_short_code);

ALTER TABLE virtual_card.sub_accounts
  ADD CONSTRAINT ux_sub_accounts_org_acct_sub_short
  UNIQUE (org_short_code, account_short_code, sub_account_short_code);

-- 3) Add foreign key constraints to maintain referential integrity
ALTER TABLE virtual_card.account
  ADD CONSTRAINT fk_account_organization_short_code
  FOREIGN KEY (org_short_code)
  REFERENCES virtual_card.organizations (org_short_code)
  ON UPDATE CASCADE
  ON DELETE RESTRICT;

ALTER TABLE virtual_card.sub_accounts
  ADD CONSTRAINT fk_sub_accounts_org_acct_short
  FOREIGN KEY (org_short_code, account_short_code)
  REFERENCES virtual_card.account (org_short_code, account_short_code)
  ON UPDATE CASCADE
  ON DELETE RESTRICT;