package com.uhg.optum.financials.payments.payer.exception;

public class ZetaRetryableException extends RuntimeException {

	private final int statusCode;

	public ZetaRetryableException(String message, int statusCode) {
		super(message);
		this.statusCode = statusCode;
	}

	public int getStatusCode() {
		return this.statusCode;
	}

}
