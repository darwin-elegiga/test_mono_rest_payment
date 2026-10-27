package com.uhg.optum.financials.payments.payer.exception;

public class NetworkPaymentIdGenerationException extends RuntimeException {

	private static final long serialVersionUID = 1L;

	private final String errorCode;

	private final String paymentId;

	public NetworkPaymentIdGenerationException(String message, String errorCode, String paymentId) {
		super(message);
		this.errorCode = errorCode;
		this.paymentId = paymentId;
	}

	public NetworkPaymentIdGenerationException(String message, String errorCode, String paymentId, Throwable cause) {
		super(message, cause);
		this.errorCode = errorCode;
		this.paymentId = paymentId;
	}

	public String getErrorCode() {
		return this.errorCode;
	}

	public String getPaymentId() {
		return this.paymentId;
	}

}
