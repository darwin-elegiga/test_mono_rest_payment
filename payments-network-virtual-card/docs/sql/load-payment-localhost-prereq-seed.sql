-- Localhost prerequisite seed data for the load-payment flow.
--
-- This script seeds the reference/master tables that must exist before calling:
--   POST /v1/payment/load
--
-- Tables like payment_request, payment_details, payee_details, shipping_details,
-- card_creations, virtual_cards, payment_request_history, and document_response_details
-- are created by the flow itself and are intentionally not inserted here.
--
-- IMPORTANT SPECIAL CASE:
--   virtual_card.bank_accounts.account_number must be stored in the encrypted-at-rest format
--   expected by EncryptionAtRestService.decrypt(...).
--   Do NOT store plain text here.
--
-- Replace these placeholders before running:
--   __ENCRYPTED_ACCOUNT_NUMBER__
--   __ENCRYPTION_KEY_VERSION__
--   __PAYER_LOGO_URL__
--   __OPTUM_LOGO_URL__
--   __MASTERCARD_LOGO_URL__
--   __INSTRUCTIONS_LOGO_URL__

BEGIN;

-- ---------------------------------------------------------------------------
-- 1. Address rows used by org/account/sub-account
-- ---------------------------------------------------------------------------
INSERT INTO virtual_card.address (id, line1, line2, city, state, country, postal_code)
VALUES
    (9001, '100 Main Street', 'Suite 100', 'Eden Prairie', 'MN', 'US', '55344'),
    (9002, '100 Main Street', 'Suite 200', 'Eden Prairie', 'MN', 'US', '55344'),
    (9003, '100 Main Street', 'Suite 300', 'Eden Prairie', 'MN', 'US', '55344')
ON CONFLICT (id) DO UPDATE
SET line1 = EXCLUDED.line1,
    line2 = EXCLUDED.line2,
    city = EXCLUDED.city,
    state = EXCLUDED.state,
    country = EXCLUDED.country,
    postal_code = EXCLUDED.postal_code;

-- ---------------------------------------------------------------------------
-- 2. Organization
-- ---------------------------------------------------------------------------
INSERT INTO virtual_card.organizations (
    org_id,
    market_type,
    org_status,
    name,
    address_id,
    tin,
    onboarded_on_dt,
    program_id,
    funding_method,
    onboarding_status,
    created_on,
    updated_on,
    created_by,
    updated_by,
    type,
    org_short_code,
    contact_name,
    contact_email,
    phone_number
)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'MEDICAL',
    'ACTIVE',
    'Localhost Test Organization',
    9001,
    '123456789',
    now(),
    'HEALTHCARE',
    'POST_FUNDED',
    'ACTIVE',
    now(),
    now(),
    'seed-script',
    'seed-script',
    'organization.registered',
    'ORGVC1',
    'Local Org Contact',
    'orgvc1@example.com',
    '8005551000'
)
ON CONFLICT (org_short_code) DO UPDATE
SET name = EXCLUDED.name,
    address_id = EXCLUDED.address_id,
    tin = EXCLUDED.tin,
    funding_method = EXCLUDED.funding_method,
    onboarding_status = EXCLUDED.onboarding_status,
    updated_on = now(),
    updated_by = EXCLUDED.updated_by,
    contact_name = EXCLUDED.contact_name,
    contact_email = EXCLUDED.contact_email,
    phone_number = EXCLUDED.phone_number;

-- ---------------------------------------------------------------------------
-- 3. Account
-- ---------------------------------------------------------------------------
INSERT INTO virtual_card.account (
    account_id,
    market_type,
    name,
    tin,
    address_id,
    type,
    account_short_code,
    org_short_code,
    contact_name,
    contact_email,
    phone_number,
    payer835_id,
    funding_method,
    onboarding_status,
    created_by_id,
    created_on,
    updated_by_id,
    updated_on
)
VALUES (
    '22222222-2222-2222-2222-222222222222',
    'MEDICAL',
    'Localhost Test Account',
    '123456789',
    9002,
    'account.registered',
    'ACCVC1',
    'ORGVC1',
    'Local Account Contact',
    'accvc1@example.com',
    '8005552000',
    'P8351',
    'POST_FUNDED',
    'ACTIVE',
    'seed-script',
    now(),
    'seed-script',
    now()
)
ON CONFLICT (org_short_code, account_short_code) DO UPDATE
SET name = EXCLUDED.name,
    address_id = EXCLUDED.address_id,
    tin = EXCLUDED.tin,
    payer835_id = EXCLUDED.payer835_id,
    funding_method = EXCLUDED.funding_method,
    onboarding_status = EXCLUDED.onboarding_status,
    updated_by_id = EXCLUDED.updated_by_id,
    updated_on = now(),
    contact_name = EXCLUDED.contact_name,
    contact_email = EXCLUDED.contact_email,
    phone_number = EXCLUDED.phone_number;

-- ---------------------------------------------------------------------------
-- 4. Bank account
-- ---------------------------------------------------------------------------
-- IMPORTANT:
--   account_number must be the encrypted-at-rest value expected by
--   EncryptionAtRestService.decrypt(...). Plain text will break ST/SE creation.
INSERT INTO virtual_card.bank_accounts (
    bank_account_id,
    created_by,
    created_on,
    updated_by,
    updated_on,
    account_number_last_four_digits,
    account_type,
    account_name,
    routing_number,
    org_id,
    type,
    verification_status,
    account_number,
    encryption_at_rest_version,
    org_short_code,
    display_name
)
VALUES (
    '44444444-4444-4444-4444-444444444444',
    'seed-script',
    now(),
    'seed-script',
    now(),
    '6789',
    'CHECKING',
    'Localhost Funding Account',
    '091000019',
    '11111111-1111-1111-1111-111111111111',
    'bank-account.created',
    'VERIFIED',
    '__ENCRYPTED_ACCOUNT_NUMBER__',
    '__ENCRYPTION_KEY_VERSION__',
    'ORGVC1',
    'Localhost Funding Account'
)
ON CONFLICT (bank_account_id) DO UPDATE
SET updated_by = EXCLUDED.updated_by,
    updated_on = now(),
    account_number_last_four_digits = EXCLUDED.account_number_last_four_digits,
    account_type = EXCLUDED.account_type,
    account_name = EXCLUDED.account_name,
    routing_number = EXCLUDED.routing_number,
    org_id = EXCLUDED.org_id,
    type = EXCLUDED.type,
    verification_status = EXCLUDED.verification_status,
    account_number = EXCLUDED.account_number,
    encryption_at_rest_version = EXCLUDED.encryption_at_rest_version,
    org_short_code = EXCLUDED.org_short_code,
    display_name = EXCLUDED.display_name;

-- ---------------------------------------------------------------------------
-- 5. Sub-account
-- ---------------------------------------------------------------------------
-- The sample setup uses POST_FUNDED, so the worker reads funding_foreign_account.
INSERT INTO virtual_card.sub_accounts (
    sub_account_id,
    market_type,
    name,
    tin,
    address_id,
    account_id,
    type,
    sub_account_short_code,
    org_short_code,
    account_short_code,
    contact_name,
    contact_email,
    phone_number,
    funding_method,
    funding_foreign_account,
    expiry_days,
    is_single_spend,
    onboarding_status,
    created_by_id,
    created_on,
    updated_by_id,
    updated_on
)
VALUES (
    '33333333-3333-3333-3333-333333333333',
    'MEDICAL',
    'Localhost Test Sub Account',
    '123456789',
    9003,
    '22222222-2222-2222-2222-222222222222',
    'sub-account.registered',
    'SUBVC1',
    'ORGVC1',
    'ACCVC1',
    'Local Sub Account Contact',
    'subvc1@example.com',
    '8005553000',
    'POST_FUNDED',
    '44444444-4444-4444-4444-444444444444',
    45,
    false,
    'ACTIVE',
    'seed-script',
    now(),
    'seed-script',
    now()
)
ON CONFLICT (org_short_code, account_short_code, sub_account_short_code) DO UPDATE
SET name = EXCLUDED.name,
    address_id = EXCLUDED.address_id,
    account_id = EXCLUDED.account_id,
    contact_name = EXCLUDED.contact_name,
    contact_email = EXCLUDED.contact_email,
    phone_number = EXCLUDED.phone_number,
    funding_method = EXCLUDED.funding_method,
    funding_foreign_account = EXCLUDED.funding_foreign_account,
    expiry_days = EXCLUDED.expiry_days,
    is_single_spend = EXCLUDED.is_single_spend,
    onboarding_status = EXCLUDED.onboarding_status,
    updated_by_id = EXCLUDED.updated_by_id,
    updated_on = now();

-- ---------------------------------------------------------------------------
-- 6. Billing address and cardholder
-- ---------------------------------------------------------------------------
INSERT INTO virtual_card.billing_addresses (id, line1, city, country, postal_code)
VALUES (1, '1 Optum Circle', 'Eden Prairie', 'US', '55344')
ON CONFLICT (id) DO UPDATE
SET line1 = EXCLUDED.line1,
    city = EXCLUDED.city,
    country = EXCLUDED.country,
    postal_code = EXCLUDED.postal_code;

INSERT INTO virtual_card.cardholders (id, first_name, last_name, email, phone_number, billing_address_id)
VALUES (1, 'Optum', 'Financial', 'payables@uhg.com', NULL, 1)
ON CONFLICT (id) DO UPDATE
SET first_name = EXCLUDED.first_name,
    last_name = EXCLUDED.last_name,
    email = EXCLUDED.email,
    phone_number = EXCLUDED.phone_number,
    billing_address_id = EXCLUDED.billing_address_id;

-- ---------------------------------------------------------------------------
-- 7. BIN
-- ---------------------------------------------------------------------------
UPDATE virtual_card.bin
SET bin = 546810,
    funding_type = 'POST_FUNDED',
    updated_at = now(),
    updated_by = 'seed-script'
WHERE funding_type = 'POST_FUNDED';

INSERT INTO virtual_card.bin (bin, funding_type, created_at, updated_at, created_by, updated_by)
SELECT 546810, 'POST_FUNDED', now(), now(), 'seed-script', 'seed-script'
WHERE NOT EXISTS (
    SELECT 1
    FROM virtual_card.bin
    WHERE funding_type = 'POST_FUNDED'
);

-- ---------------------------------------------------------------------------
-- 8. Optional spend rules
-- ---------------------------------------------------------------------------
-- This table is optional for a basic load-payment flow.
-- Uncomment only if you want deterministic local spend rules.
--
-- UPDATE virtual_card.spend_rules
-- SET rule_code = ARRAY['RS1-1005', 'RS1-1025'],
--     org_name = 'Localhost Test Organization',
--     account_name = 'Localhost Test Account',
--     sub_account_name = 'Localhost Test Sub Account',
--     updated_at = now(),
--     updated_by = 'seed-script'
-- WHERE org_short_code = 'ORGVC1'
--   AND account_short_code = 'ACCVC1'
--   AND sub_account_short_code = 'SUBVC1';
--
-- INSERT INTO virtual_card.spend_rules (
--     rule_code,
--     org_short_code,
--     account_short_code,
--     sub_account_short_code,
--     org_name,
--     account_name,
--     sub_account_name,
--     created_at,
--     created_by,
--     updated_at,
--     updated_by
-- )
-- SELECT
--     ARRAY['RS1-1005', 'RS1-1025'],
--     'ORGVC1',
--     'ACCVC1',
--     'SUBVC1',
--     'Localhost Test Organization',
--     'Localhost Test Account',
--     'Localhost Test Sub Account',
--     now(),
--     'seed-script',
--     now(),
--     'seed-script'
-- WHERE NOT EXISTS (
--     SELECT 1
--     FROM virtual_card.spend_rules
--     WHERE org_short_code = 'ORGVC1'
--       AND account_short_code = 'ACCVC1'
--       AND sub_account_short_code = 'SUBVC1'
-- );

-- ---------------------------------------------------------------------------
-- 9. Summary-page custom remarks
-- ---------------------------------------------------------------------------
UPDATE virtual_card.virtual_card_document_custom_remarks
SET custom_remarks1 = 'Electronic Provider Remittance Advice',
    custom_remarks2 = '1. Process this Mastercard payment immediately.
2. If there is a problem receiving this payment, contact the payer support team.
3. Keep the Payment ID available when calling support.',
    created_at = now()
WHERE org_short_code = 'ORGVC1'
  AND account_short_code = 'ACCVC1'
  AND sub_account_short_code = 'SUBVC1';

INSERT INTO virtual_card.virtual_card_document_custom_remarks (
    org_short_code,
    account_short_code,
    sub_account_short_code,
    custom_remarks1,
    custom_remarks2,
    created_at
)
SELECT
    'ORGVC1',
    'ACCVC1',
    'SUBVC1',
    'Electronic Provider Remittance Advice',
    '1. Process this Mastercard payment immediately.
2. If there is a problem receiving this payment, contact the payer support team.
3. Keep the Payment ID available when calling support.',
    now()
WHERE NOT EXISTS (
    SELECT 1
    FROM virtual_card.virtual_card_document_custom_remarks
    WHERE org_short_code = 'ORGVC1'
      AND account_short_code = 'ACCVC1'
      AND sub_account_short_code = 'SUBVC1'
);

-- ---------------------------------------------------------------------------
-- 10. Payer logo used by summary-page generation
-- ---------------------------------------------------------------------------
UPDATE virtual_card.logo_details
SET logo_url = '__PAYER_LOGO_URL__',
    type = 'logo_updated',
    updated_by_id = 'seed-script',
    last_update = now()
WHERE org_short_code = 'ORGVC1'
  AND account_short_code = 'ACCVC1'
  AND sub_account_short_code = 'SUBVC1';

INSERT INTO virtual_card.logo_details (
    id,
    created_by_id,
    created_on,
    updated_by_id,
    last_update,
    logo_url,
    type,
    org_short_code,
    account_short_code,
    sub_account_short_code
)
SELECT
    '55555555-5555-5555-5555-555555555555',
    'seed-script',
    now(),
    'seed-script',
    now(),
    '__PAYER_LOGO_URL__',
    'logo_updated',
    'ORGVC1',
    'ACCVC1',
    'SUBVC1'
WHERE NOT EXISTS (
    SELECT 1
    FROM virtual_card.logo_details
    WHERE org_short_code = 'ORGVC1'
      AND account_short_code = 'ACCVC1'
      AND sub_account_short_code = 'SUBVC1'
);

-- ---------------------------------------------------------------------------
-- 11. Company logos used by the HTML template
-- ---------------------------------------------------------------------------
UPDATE virtual_card.company_logo_details
SET logo_url = '__OPTUM_LOGO_URL__',
    updated_by_id = 'seed-script',
    last_update = now()
WHERE type = 'OPTUM';

INSERT INTO virtual_card.company_logo_details (
    id,
    logo_url,
    type,
    created_by_id,
    created_on,
    updated_by_id,
    last_update
)
SELECT
    '66666666-6666-6666-6666-666666666666',
    '__OPTUM_LOGO_URL__',
    'OPTUM',
    'seed-script',
    now(),
    'seed-script',
    now()
WHERE NOT EXISTS (
    SELECT 1
    FROM virtual_card.company_logo_details
    WHERE type = 'OPTUM'
);

UPDATE virtual_card.company_logo_details
SET logo_url = '__MASTERCARD_LOGO_URL__',
    updated_by_id = 'seed-script',
    last_update = now()
WHERE type = 'MASTERCARD';

INSERT INTO virtual_card.company_logo_details (
    id,
    logo_url,
    type,
    created_by_id,
    created_on,
    updated_by_id,
    last_update
)
SELECT
    '77777777-7777-7777-7777-777777777777',
    '__MASTERCARD_LOGO_URL__',
    'MASTERCARD',
    'seed-script',
    now(),
    'seed-script',
    now()
WHERE NOT EXISTS (
    SELECT 1
    FROM virtual_card.company_logo_details
    WHERE type = 'MASTERCARD'
);

UPDATE virtual_card.company_logo_details
SET logo_url = '__INSTRUCTIONS_LOGO_URL__',
    updated_by_id = 'seed-script',
    last_update = now()
WHERE type = 'INSTRUCTIONS';

INSERT INTO virtual_card.company_logo_details (
    id,
    logo_url,
    type,
    created_by_id,
    created_on,
    updated_by_id,
    last_update
)
SELECT
    '88888888-8888-8888-8888-888888888888',
    '__INSTRUCTIONS_LOGO_URL__',
    'INSTRUCTIONS',
    'seed-script',
    now(),
    'seed-script',
    now()
WHERE NOT EXISTS (
    SELECT 1
    FROM virtual_card.company_logo_details
    WHERE type = 'INSTRUCTIONS'
);

COMMIT;
