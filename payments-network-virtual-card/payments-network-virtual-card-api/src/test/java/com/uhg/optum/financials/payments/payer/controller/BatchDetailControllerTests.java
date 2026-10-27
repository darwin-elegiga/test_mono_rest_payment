package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.PaymentRequestTotalDto;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.BatchDetail;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_REQUEST_NOT_FOUND_BY_CORRELATION_ID_ERROR;
import static org.mockito.BDDMockito.given;
import static org.mockito.ArgumentMatchers.eq;

@ExtendWith(MockitoExtension.class)
public class BatchDetailControllerTests {

	@InjectMocks
	BatchDetailController batchDetailController;

	@Mock
	BatchDetailService batchDetailService;

	@Test
	void getPaymentCountAndAmountByStatus_returnsDtoForValidCorrelationId() {
		String correlationId = "test-correlation-id";
		PaymentRequestTotalDto dto = new PaymentRequestTotalDto();
		given(this.batchDetailService.getPaymentCountAndAmountByStatus(eq(correlationId))).willReturn(dto);

		PaymentRequestTotalDto result = this.batchDetailController.getPaymentCountAndAmountByStatus(correlationId);
		Assertions.assertThat(result).isEqualTo(dto);
	}

	@Test
	void getPaymentCountAndAmountByStatus_throwsNotFoundForUnknownCorrelationId() {
		String correlationId = "unknown-id";
		given(this.batchDetailService.getPaymentCountAndAmountByStatus(eq(correlationId)))
			.willThrow(new NotFoundException(PAYMENT_REQUEST_NOT_FOUND_BY_CORRELATION_ID_ERROR + correlationId));

		Assertions.assertThatThrownBy(() -> this.batchDetailController.getPaymentCountAndAmountByStatus(correlationId))
			.isInstanceOf(NotFoundException.class)
			.hasMessageContaining(PAYMENT_REQUEST_NOT_FOUND_BY_CORRELATION_ID_ERROR + correlationId);
	}

	@Test
	void getPaymentCountAndAmountByStatus_throwsExceptionForServiceError() {
		String correlationId = "error-id";
		given(this.batchDetailService.getPaymentCountAndAmountByStatus(eq(correlationId)))
			.willThrow(new IllegalArgumentException("Invalid correlation id"));

		Assertions.assertThatThrownBy(() -> this.batchDetailController.getPaymentCountAndAmountByStatus(correlationId))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid correlation id");
	}

	@Test
	void getBatchDetailsByCorrelationId_returnsBatchDetails() {
		String correlationId = "test-correlation-id";
		BatchDetail batchDetail = new BatchDetail();
		batchDetail.setCorrelationId(correlationId);
		List<BatchDetail> batchDetails = List.of(batchDetail);
		given(this.batchDetailService.getBatchDetailsByCorrelationId(eq(correlationId))).willReturn(batchDetails);

		List<BatchDetail> result = this.batchDetailController.getBatchDetailsByCorrelationId(correlationId);

		Assertions.assertThat(result).isEqualTo(batchDetails);
	}

}
