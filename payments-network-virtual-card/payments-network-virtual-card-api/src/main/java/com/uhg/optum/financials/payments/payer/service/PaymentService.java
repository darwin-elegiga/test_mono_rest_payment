package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.FileTrackingDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentResponseDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;

public interface PaymentService {

	void loadPayment(PaymentRequestDto paymentRequestDto);

	PaymentResponseDto getPaymentRequestDetail(String paymentID);

	void reProcessPayment(PaymentRequestDto paymentRequestDto);

	FileTrackingDto getFileName(String correlationId);

}
