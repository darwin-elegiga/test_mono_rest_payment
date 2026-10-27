package com.uhg.optum.financials.payments.payer.exception;

/**
 * Raised when an event could not be handed off to the storage queue.
 *
 * @author Sachin Magar
 */
public class QueuePublishException extends RuntimeException {

	public QueuePublishException(String message, Throwable cause) {
		super(message, cause);
	}

}
