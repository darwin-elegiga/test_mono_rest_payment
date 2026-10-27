package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.producer.StorageQueueProducerApi;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class ResetCardServiceImplTests {

	@InjectMocks
	ResetCardServiceImpl resetCardService;

	@Mock
	AuthExtractRepository authExtractRepository;

	@Mock
	LogUtility logUtility;

	@Mock
	StorageQueueProducerApi storageQueueProducer;

	@Mock
	AuditLogger auditLogger;

	@Mock
	PaymentRequestRepository paymentRequestRepository;

	@Test
	void testResetCardException() {
		ResetCardRequest resetCardRequest = new ResetCardRequest();
		BDDMockito.when(this.paymentRequestRepository.findByPaymentId(any())).thenReturn(new PaymentRequest());

		Assertions.assertThatThrownBy(() -> this.resetCardService.sendResetCardRequest(resetCardRequest))
			.isInstanceOf(BadRequestException.class);
	}

	// @Test
	void testResetCard() {
		ResetCardRequest resetCardRequest = new ResetCardRequest();
		BDDMockito.when(this.authExtractRepository.findCardIdByReceiptId(any())).thenReturn(UUID.randomUUID());
		BDDMockito.when(this.paymentRequestRepository.findByPaymentId(any())).thenReturn(new PaymentRequest());
		this.resetCardService.sendResetCardRequest(resetCardRequest);
	}

	@Test
	void testResetCardNotFoundException() {
		ResetCardRequest resetCardRequest = new ResetCardRequest();
		BDDMockito.when(this.paymentRequestRepository.findByPaymentId(any())).thenReturn(null);

		Assertions.assertThatThrownBy(() -> this.resetCardService.sendResetCardRequest(resetCardRequest))
			.isInstanceOf(NotFoundException.class);
	}

}
