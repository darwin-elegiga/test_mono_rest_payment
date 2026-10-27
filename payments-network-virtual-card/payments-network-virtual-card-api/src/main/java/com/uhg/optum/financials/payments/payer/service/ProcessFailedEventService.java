package com.uhg.optum.financials.payments.payer.service;

public interface ProcessFailedEventService {

	void processFailedEvent(String paymentId, String correlationId);

}
