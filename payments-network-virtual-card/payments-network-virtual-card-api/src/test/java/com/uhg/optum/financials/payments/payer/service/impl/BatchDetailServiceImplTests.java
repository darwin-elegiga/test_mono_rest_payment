package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.PaymentRequestTotalDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.BatchDetail;
import com.uhg.optum.financials.payments.payer.repository.BatchDetailRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import java.math.BigDecimal;
import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CORRELATION_ID_REQUIRED_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_NOT_FOUND_BY_CORRELATION_ID_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_REQUEST_NOT_FOUND_BY_CORRELATION_ID_ERROR;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.ArgumentMatchers.eq;

@ExtendWith(MockitoExtension.class)
public class BatchDetailServiceImplTests {

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private BatchDetailRepository batchDetailRepository;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private BatchDetailServiceImpl batchDetailServiceImpl;

	@Test
	void getPaymentCountAndAmountByStatus_multipleEntries() {
		String correlationId = "corrId";
		List<PaymentRequestTotalDto.PaymentRequest> payments = List.of(
				new PaymentRequestTotalDto.PaymentRequest("PI", "payment-1", new BigDecimal("20.00")),
				new PaymentRequestTotalDto.PaymentRequest("PI", "payment-2", new BigDecimal("30.00")));
		given(this.paymentRequestRepository.findPaymentRequestsByCorrelationId(eq(correlationId))).willReturn(payments);

		PaymentRequestTotalDto result = this.batchDetailServiceImpl.getPaymentCountAndAmountByStatus(correlationId);
		Assertions.assertThat(result.getPaymentRequest()).isEqualTo(payments);
		Assertions.assertThat(result.getTotalPaymentCount()).isEqualTo("2");
		then(this.logUtility).should()
			.logMessage(eq("Fetching payment details for correlationId: " + correlationId), eq("INFO"),
					org.mockito.ArgumentMatchers.any());

	}

	@Test
	void getPaymentCountAndAmountByStatus_throwsNotFoundWhenCorrelationIdDoesNotExist() {
		String correlationId = "emptyCorrId";
		given(this.paymentRequestRepository.findPaymentRequestsByCorrelationId(eq(correlationId)))
			.willReturn(List.of());

		Assertions.assertThatThrownBy(() -> this.batchDetailServiceImpl.getPaymentCountAndAmountByStatus(correlationId))
			.isInstanceOf(NotFoundException.class)
			.hasMessageContaining(PAYMENT_REQUEST_NOT_FOUND_BY_CORRELATION_ID_ERROR + correlationId);
	}

	@Test
	void getPaymentCountAndAmountByStatus_throwsBadRequestWhenCorrelationIdIsBlank() {
		Assertions.assertThatThrownBy(() -> this.batchDetailServiceImpl.getPaymentCountAndAmountByStatus(" "))
			.isInstanceOf(BadRequestException.class)
			.hasMessageContaining(CORRELATION_ID_REQUIRED_ERROR);
	}

	@Test
	void constructor_initializesFields() {
		BatchDetailServiceImpl service = new BatchDetailServiceImpl(this.paymentRequestRepository,
				this.batchDetailRepository, this.logUtility);
		Assertions.assertThat(service).isNotNull();
	}

	@Test
	void getBatchDetailsByCorrelationId_returnsBatchDetails() {
		String correlationId = "corrId";
		BatchDetail batchDetail = new BatchDetail();
		batchDetail.setCorrelationId(correlationId);
		List<BatchDetail> batchDetails = List.of(batchDetail);
		given(this.batchDetailRepository.findByCorrelationId(eq(correlationId))).willReturn(batchDetails);

		List<BatchDetail> result = this.batchDetailServiceImpl.getBatchDetailsByCorrelationId(correlationId);

		Assertions.assertThat(result).isEqualTo(batchDetails);
		then(this.batchDetailRepository).should().findByCorrelationId(correlationId);
	}

	@Test
	void getBatchDetailsByCorrelationId_throwsBadRequestWhenCorrelationIdIsBlank() {
		Assertions.assertThatThrownBy(() -> this.batchDetailServiceImpl.getBatchDetailsByCorrelationId(" "))
			.isInstanceOf(BadRequestException.class)
			.hasMessageContaining(CORRELATION_ID_REQUIRED_ERROR);
	}

	@Test
	void getBatchDetailsByCorrelationId_throwsNotFoundWhenNoBatchDetailsExist() {
		String correlationId = "emptyCorrId";
		given(this.batchDetailRepository.findByCorrelationId(eq(correlationId))).willReturn(List.of());

		Assertions.assertThatThrownBy(() -> this.batchDetailServiceImpl.getBatchDetailsByCorrelationId(correlationId))
			.isInstanceOf(NotFoundException.class)
			.hasMessageContaining(BATCH_DETAIL_NOT_FOUND_BY_CORRELATION_ID_ERROR + correlationId);
	}

}
