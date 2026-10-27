package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;

public interface PaymentRequestService {

	void processPaymentRequestEvents(PaymentRequestDto paymentRequest);

}
