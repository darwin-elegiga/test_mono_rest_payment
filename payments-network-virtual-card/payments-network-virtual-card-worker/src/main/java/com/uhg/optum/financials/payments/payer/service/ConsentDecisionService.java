package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;

public interface ConsentDecisionService {

	boolean determineAutoActivateCard(PaymentRequestDto paymentRequestDto);

}
