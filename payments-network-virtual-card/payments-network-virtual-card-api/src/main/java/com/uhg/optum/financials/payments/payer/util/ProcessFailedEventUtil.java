package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.exception.BadRequestException;

import com.uhg.optum.financials.payments.payer.repository.BatchDetailRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import lombok.AllArgsConstructor;
import org.springframework.stereotype.Component;

@Component
@AllArgsConstructor
public class ProcessFailedEventUtil {

	private final PaymentRequestRepository paymentRequestRepository;

	private final BatchDetailRepository batchDetailRepository;

	public void checkIfPaymentIdNotAvailable(String paymentId) {
		if (!this.paymentRequestRepository.existsByPaymentDetailsId(paymentId)) {
			throw new BadRequestException("PaymentId not available");
		}
	}

}
