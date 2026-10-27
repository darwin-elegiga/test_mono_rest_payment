-- Support resumable card-issuance completion processing: track the Zeta-confirmed
-- card guid and completion-pipeline progress on card_creations so a reprocess
-- attempt can resume from exactly the step it last stopped at instead of
-- repeating already-completed work (virtual card creation, document generation,
-- STP remittance, completion webhook).
ALTER TABLE virtual_card.card_creations ADD COLUMN card_guid uuid NULL;
ALTER TABLE virtual_card.card_creations ADD COLUMN completion_stage varchar(32) NULL;
