package com.uhg.optum.financials.payments.payer.service;

public interface LoadPaymentCardCallbackService {

	void pollAndCallback(String paymentId);

}
