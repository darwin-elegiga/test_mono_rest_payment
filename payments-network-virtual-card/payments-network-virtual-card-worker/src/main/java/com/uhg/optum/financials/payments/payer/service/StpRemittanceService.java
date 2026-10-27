package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.model.PaymentRequest;

public interface StpRemittanceService {

	void sendRemittance(PaymentRequest paymentRequest);

}
