package com.uhg.optum.financials.payments.payer.exception;

import org.springframework.web.client.RestClientException;

public class ZetaRestClientException extends RestClientException {

	private final int statusCode;

	public ZetaRestClientException(String msg, int statusCode) {
		super(msg);
		this.statusCode = statusCode;
	}

	public int getStatusCode() {
		return this.statusCode;
	}

}
