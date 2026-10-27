package com.uhg.optum.financials.payments.payer.exception;

public class JobAlreadyRunningException extends RuntimeException {

	public JobAlreadyRunningException() {
		super();
	}

	public JobAlreadyRunningException(String message) {
		super(message);
	}

}
