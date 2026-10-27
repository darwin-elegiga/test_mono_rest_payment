package com.uhg.optum.financials.payments.payer.exception;

import org.springframework.http.HttpStatusCode;

public class ClientErrorException extends RuntimeException {

	private final int statusCode;

	public ClientErrorException(String message, HttpStatusCode httpStatus) {
		super(message);
		this.statusCode = httpStatus.value();
	}

	public int getStatusCode() {
		return this.statusCode;
	}

}
