package com.uhg.optum.financials.payments.payer.enums;

/**
 * Represents the type of card-related event being processed. Helps categorize audit
 * entries and operational flows.
 *
 * @author Ankit Lawaniya
 */
public enum CardEventType {

	CREATE, // Card creation request
	CANCEL, // Card cancellation request
	RESET, // Card reset request
	AUTH, // Authorization event
	CLEARING // Clearing event

}
