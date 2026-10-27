package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.ReprocessRequestDto;
import com.uhg.optum.financials.payments.payer.service.ReprocessPaymentService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;

@ExtendWith(MockitoExtension.class)
public class PaymentReProcessingControllerTests {

	@InjectMocks
	PaymentReProcessingController paymentReProcessingController;

	@Mock
	LogUtility logUtility;

	@Mock
	ReprocessPaymentService reprocessPaymentService;

	private ReprocessRequestDto withCorrelationId(String correlationId) {
		ReprocessRequestDto request = new ReprocessRequestDto();
		request.setCorrelationId(correlationId);
		return request;
	}

	private ReprocessRequestDto withPaymentId(String paymentId) {
		ReprocessRequestDto request = new ReprocessRequestDto();
		request.setPaymentId(paymentId);
		return request;
	}

	@Test
	void reprocessPaymentTest() {
		Mockito.when(this.reprocessPaymentService.getReprocessPaymentRequestCount("123", null)).thenReturn(1);
		Mockito.when(this.reprocessPaymentService.tryReprocessPaymentRequest("123", null)).thenReturn(true);
		Assertions
			.assertThat(this.paymentReProcessingController.reprocessPayment(withCorrelationId("123"))
				.getStatusCode()
				.is2xxSuccessful())
			.isTrue();
	}

	@Test
	void reprocessPayment_NoPaymentTest() {
		Mockito.when(this.reprocessPaymentService.getReprocessPaymentRequestCount("123", null)).thenReturn(0);
		Assertions
			.assertThat(this.paymentReProcessingController.reprocessPayment(withCorrelationId("123"))
				.getStatusCode()
				.is2xxSuccessful())
			.isFalse();
		Mockito.verify(this.reprocessPaymentService, Mockito.never())
			.tryReprocessPaymentRequest(Mockito.any(), Mockito.any());
	}

	@Test
	void reprocessPayment_AlreadyInProgress_ReturnsConflict() {
		Mockito.when(this.reprocessPaymentService.getReprocessPaymentRequestCount("123", null)).thenReturn(1);
		Mockito.when(this.reprocessPaymentService.tryReprocessPaymentRequest("123", null)).thenReturn(false);
		Assertions
			.assertThat(this.paymentReProcessingController.reprocessPayment(withCorrelationId("123")).getStatusCode())
			.isEqualTo(HttpStatus.CONFLICT);
	}

	@Test
	void reprocessPayment_ByPaymentId_Test() {
		Mockito.when(this.reprocessPaymentService.getReprocessPaymentRequestCount(null, "payment-123")).thenReturn(1);
		Mockito.when(this.reprocessPaymentService.tryReprocessPaymentRequest(null, "payment-123")).thenReturn(true);
		Assertions
			.assertThat(this.paymentReProcessingController.reprocessPayment(withPaymentId("payment-123"))
				.getStatusCode()
				.is2xxSuccessful())
			.isTrue();
		Mockito.verify(this.reprocessPaymentService).tryReprocessPaymentRequest(null, "payment-123");
	}

	@Test
	void reprocessPayment_ByPaymentId_NoPaymentTest() {
		Mockito.when(this.reprocessPaymentService.getReprocessPaymentRequestCount(null, "payment-123")).thenReturn(0);
		Assertions
			.assertThat(
					this.paymentReProcessingController.reprocessPayment(withPaymentId("payment-123")).getStatusCode())
			.isEqualTo(HttpStatus.NOT_FOUND);
	}

	@Test
	void reprocessPayment_ByPaymentId_AlreadyInProgress_ReturnsConflict() {
		Mockito.when(this.reprocessPaymentService.getReprocessPaymentRequestCount(null, "payment-123")).thenReturn(1);
		Mockito.when(this.reprocessPaymentService.tryReprocessPaymentRequest(null, "payment-123")).thenReturn(false);
		Assertions
			.assertThat(
					this.paymentReProcessingController.reprocessPayment(withPaymentId("payment-123")).getStatusCode())
			.isEqualTo(HttpStatus.CONFLICT);
	}

	@Test
	void reprocessPayment_NeitherProvided_ReturnsBadRequest() {
		ReprocessRequestDto request = new ReprocessRequestDto();
		Assertions.assertThat(this.paymentReProcessingController.reprocessPayment(request).getStatusCode())
			.isEqualTo(HttpStatus.BAD_REQUEST);
		Mockito.verifyNoInteractions(this.reprocessPaymentService);
	}

	@Test
	void reprocessPayment_BothProvided_ReturnsBadRequest() {
		ReprocessRequestDto request = new ReprocessRequestDto();
		request.setCorrelationId("123");
		request.setPaymentId("payment-123");
		Assertions.assertThat(this.paymentReProcessingController.reprocessPayment(request).getStatusCode())
			.isEqualTo(HttpStatus.BAD_REQUEST);
		Mockito.verifyNoInteractions(this.reprocessPaymentService);
	}

	@Test
	void reprocessPayment_BlankCorrelationIdWithPaymentId_IsTreatedAsPaymentIdOnly() {
		// A blank correlationId must not be forwarded as-is — it has to be normalized to
		// null so the service doesn't mistake it for a real (empty) correlation ID and
		// take the wrong branch instead of the payment-ID one the caller intended.
		ReprocessRequestDto request = new ReprocessRequestDto();
		request.setCorrelationId("");
		request.setPaymentId("payment-123");

		Mockito.when(this.reprocessPaymentService.getReprocessPaymentRequestCount(null, "payment-123")).thenReturn(1);
		Mockito.when(this.reprocessPaymentService.tryReprocessPaymentRequest(null, "payment-123")).thenReturn(true);

		Assertions
			.assertThat(this.paymentReProcessingController.reprocessPayment(request).getStatusCode().is2xxSuccessful())
			.isTrue();
		Mockito.verify(this.reprocessPaymentService).getReprocessPaymentRequestCount(null, "payment-123");
		Mockito.verify(this.reprocessPaymentService).tryReprocessPaymentRequest(null, "payment-123");
	}

}
