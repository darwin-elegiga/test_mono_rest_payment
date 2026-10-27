package com.uhg.optum.financials.payments.payer.exception;

/**
 * Signals that building the ST/SE EDI segment failed. This is always a local,
 * deterministic transformation over already-known payment data — never a call to an
 * external dependency — so retrying the identical input (e.g. via Storage Queue
 * redelivery) cannot succeed where it just failed. Callers should treat this as
 * non-retryable, not a transient condition.
 *
 * @author Sreekar basetty
 */
public class SegmentGenerationException extends RuntimeException {

	public SegmentGenerationException(String message, Throwable cause) {
		super(message, cause);
	}

}
