package com.uhg.optum.financials.payments.payer.exception;

public class CardProcessingError extends RuntimeException {

	public CardProcessingError(String message) {
		super(message);
	}

	public CardProcessingError(String message, Throwable cause) {
		super(message, cause);
	}

}
