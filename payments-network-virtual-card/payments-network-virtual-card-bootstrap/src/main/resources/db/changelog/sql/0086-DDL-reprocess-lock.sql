-- Prevents concurrent reprocess triggers for the same payment or correlation from both
-- reading the same stale PE/CardCreation state and both dispatching — the identifier is
-- namespaced ("payment:"/"correlation:") so the two scopes can never collide in one table.
-- A status column (rather than row presence/absence) is used so every trigger/release is
-- kept as a durable audit trail instead of being discarded.
CREATE TABLE IF NOT EXISTS virtual_card.reprocess_lock (
    identifier   VARCHAR(255) NOT NULL,
    status       VARCHAR(20)  NOT NULL DEFAULT 'AVAILABLE',
    triggered_at TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    released_at  TIMESTAMP(6) NULL,
    CONSTRAINT reprocess_lock_pkey PRIMARY KEY (identifier)
);
