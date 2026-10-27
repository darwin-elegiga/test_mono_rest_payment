-- The same BIN value can legitimately back multiple funding types, but each funding type
-- must resolve to exactly one row for card creation.
WITH ranked_bins AS (
    SELECT id,
           ROW_NUMBER() OVER (
               PARTITION BY funding_type
               ORDER BY updated_at DESC NULLS LAST, created_at DESC NULLS LAST, id DESC
           ) AS row_num
    FROM virtual_card.bin
    WHERE funding_type IS NOT NULL
)
DELETE FROM virtual_card.bin target
USING ranked_bins duplicate
WHERE target.id = duplicate.id
  AND duplicate.row_num > 1;

ALTER TABLE virtual_card.bin
    ADD CONSTRAINT uk_bin_funding_type UNIQUE (funding_type);
