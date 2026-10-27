ALTER TABLE virtual_card.consent_required_states
ADD COLUMN created_by VARCHAR(100) NOT NULL DEFAULT 'system',
ADD COLUMN updated_by VARCHAR(100) NOT NULL DEFAULT 'system';
