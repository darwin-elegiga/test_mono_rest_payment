package com.uhg.optum.financials.payments.payer.enums;

/**
 * Tracks progress through the post-card-issuance completion workflow (virtual card
 * creation, completion webhook, summary document generation, STP remittance) so that a
 * reprocessing attempt can resume from exactly the step it last stopped at instead of
 * repeating work that already succeeded.
 *
 * <p>
 * Declaration order here MUST match the actual execution order in
 * {@code CreateCardWebhookHandler.process()} — {@link #isBefore(CompletionStage)}
 * compares ordinals, so a step declared "later" than one it actually runs before would
 * cause that earlier step to be wrongly skipped on resume.
 *
 * @author sreekar basetty
 */
public enum CompletionStage {

	ISSUANCE_CONFIRMED, VIRTUAL_CARD_CREATED, COMPLETION_WEBHOOK_SENT, DOCUMENT_GENERATED, STP_COMPLETED, COMPLETED;

	public boolean isBefore(CompletionStage other) {
		return this.ordinal() < other.ordinal();
	}

}
