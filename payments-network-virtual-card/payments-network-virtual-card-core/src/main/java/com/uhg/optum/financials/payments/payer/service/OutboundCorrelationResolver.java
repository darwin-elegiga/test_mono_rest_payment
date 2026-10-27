package com.uhg.optum.financials.payments.payer.service;

public interface OutboundCorrelationResolver {

	String resolveActualCorrelationId(String processingCorrelationId);

}
