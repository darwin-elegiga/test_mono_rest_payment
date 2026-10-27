ALTER TABLE virtual_card.virtual_cards
    ADD COLUMN IF NOT EXISTS provider_tin VARCHAR(20),
    ADD COLUMN IF NOT EXISTS provider_state VARCHAR(2),
    ADD COLUMN IF NOT EXISTS activation_status VARCHAR(30),
    ADD COLUMN IF NOT EXISTS activation_reason VARCHAR(50),
    ADD COLUMN IF NOT EXISTS activation_status_updated_at TIMESTAMP;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'chk_virtual_cards_activation_status'
            AND conrelid = 'virtual_card.virtual_cards'::regclass
    ) THEN
        ALTER TABLE virtual_card.virtual_cards
            ADD CONSTRAINT chk_virtual_cards_activation_status
                CHECK (
                    activation_status IN (
                        'ACTIVE',
                        'INACTIVE',
                        'ACTIVATING',
                        'DEACTIVATING',
                        'ACTIVATION_FAILED',
                        'DEACTIVATION_FAILED'
                    )
                );
    END IF;
END
$$;
