package com.uhg.optum.financials.payments.payer.enums;

/**
 * Represents the origin of a card-related event. Used to identify whether the event was
 * triggered by an external system, internal service, or the card provider (Zeta).
 *
 * @author Ankit Lawaniya
 */
public enum CardEventSource {

	ISSUING, // External system initiating the request
	VIRTUAL_CARD, // Internal service or module
	ZETA // External card provider sending response or webhook

}
