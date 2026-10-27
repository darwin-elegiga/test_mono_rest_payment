package com.uhg.optum.financials.payments.payer.enums;

/**
 * Represents the lifecycle status of a card operation. Used in audit logging to track the
 * progress and outcome of operations like creation, cancellation, and reset.
 *
 * @author Ankit Lawaniya
 */
public enum CardOperationStatus {

	REQUEST_RECEIVED("Request Received from Issuing"), REQUEST_INITIATED("Request sent to Zeta"),
	REQUEST_ACCEPTED("Zeta responded positively"), REQUEST_REJECTED("Zeta rejected the request"),
	CARD_ISSUED("Card successfully created via Webhook"), CARD_ISSUE_FAILED("Card creation failed via Webhook"),
	CARD_CANCELLED("Card successfully cancelled via Webhook"),
	CARD_CANCELLATION_FAILED("Card cancellation failed via Webhook"),
	EXCEPTION_OCCURRED("Exception occurred during processing"),
	REQUEST_PROCESSING_FAILED("Failure occurred while processing the request internally"),
	CALLBACK_PROCESSING_FAILED("Failure occurred while processing the webhook callback internally"),
	ERROR_OCCURRED("Error occurred during processing"), AUTH_REQUEST_ACCEPTED("Authorization request accepted"),
	AUTH_REQUEST_REJECTED("Authorization request rejected"), AUTH_RESET("Authorization reset successfully"),
	AUTH_RESET_FAILED("Authorization reset failed");

	private final String description;

	CardOperationStatus(String description) {
		this.description = description;
	}

	public String getDescription() {
		return this.description;
	}

}
