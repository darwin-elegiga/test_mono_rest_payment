-- ============================================================================
-- Migration: 0084-ALTER-cardholder-billing-address-columns-to-255.sql
-- Date: 2026-09-24
-- Purpose: Increase column sizes to 255 characters to align with @FreeTextValid constraint
-- ============================================================================
-- ALTER TABLE virtual_card.cardholders
-- Increase firstName and lastName columns to 255 characters
ALTER TABLE virtual_card.cardholders
ALTER COLUMN first_name TYPE VARCHAR(255);

ALTER TABLE virtual_card.cardholders
ALTER COLUMN last_name TYPE VARCHAR(255);

-- ALTER TABLE virtual_card.billing_addresses
-- Increase line1, city, and postal_code columns to 255 characters
ALTER TABLE virtual_card.billing_addresses
ALTER COLUMN line1 TYPE VARCHAR(255);

ALTER TABLE virtual_card.billing_addresses
ALTER COLUMN city TYPE VARCHAR(255);

ALTER TABLE virtual_card.billing_addresses
ALTER COLUMN postal_code TYPE VARCHAR(255);

-- ============================================================================
-- Rationale:
-- The application uses @FreeTextValid annotation to validate string fields
-- with a maximum of 255 characters. Database schema must support this
-- constraint to prevent data loss or constraint violations.
-- ============================================================================
