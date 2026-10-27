-- Fix incorrect seeded BIN values for funding types.
-- This is idempotent: it only updates rows where the old bad BIN (111111) is present.

UPDATE virtual_card.bin
SET bin = 546810,
    updated_at = now(),
    updated_by = 'system'
WHERE funding_type = 'POST_FUNDED'
  AND bin = 111111;

UPDATE virtual_card.bin
SET bin = 548130,
    updated_at = now(),
    updated_by = 'system'
WHERE funding_type = 'PRE_FUNDED_NEXT_DAY'
  AND bin = 111111;

UPDATE virtual_card.bin
SET bin = 548130,
    updated_at = now(),
    updated_by = 'system'
WHERE funding_type = 'PRE_FUNDED_SAME_DAY'
  AND bin = 111111;