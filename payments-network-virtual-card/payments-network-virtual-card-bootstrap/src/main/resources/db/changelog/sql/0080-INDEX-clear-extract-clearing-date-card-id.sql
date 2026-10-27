-- Supports monthly bounded reads for vc_reporting.load_mc_monthly_report()
-- and avoids full clear_extract scans during catch-up months.
CREATE INDEX IF NOT EXISTS idx_clear_extract_clearing_date_card_id
    ON virtual_card.clear_extract (clearing_date, card_id);

