package com.uhg.optum.financials.payments.payer.enums;

/**
 * Represents the final state of a virtual card in the master table. Reflects the outcome
 * of operations like creation, cancellation, or reset.
 *
 * @author Ankit Lawaniya
 */
public enum VirtualCardState {

	CREATED, // Card successfully created
	CANCELLED, // Card successfully cancelled
	RESET // Card successfully reset

}
