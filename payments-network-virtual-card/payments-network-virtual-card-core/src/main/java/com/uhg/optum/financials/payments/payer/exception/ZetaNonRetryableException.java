package com.uhg.optum.financials.payments.payer.exception;

public class ZetaNonRetryableException extends RuntimeException {

	private final int statusCode;

	public ZetaNonRetryableException(String message, int statusCode) {
		super(message);
		this.statusCode = statusCode;
	}

	public int getStatusCode() {
		return this.statusCode;
	}

}
