package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.PaymentRequestTotalDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.BatchDetail;
import com.uhg.optum.financials.payments.payer.repository.BatchDetailRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;
import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CORRELATION_ID_REQUIRED_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_NOT_FOUND_BY_CORRELATION_ID_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_REQUEST_NOT_FOUND_BY_CORRELATION_ID_ERROR;

@Slf4j
@Service
public class BatchDetailServiceImpl implements BatchDetailService {

	private final PaymentRequestRepository paymentRequestRepository;

	private final BatchDetailRepository batchDetailRepository;

	private final LogUtility logUtility;

	public BatchDetailServiceImpl(PaymentRequestRepository paymentRequestRepository,
			BatchDetailRepository batchDetailRepository, LogUtility logUtility) {
		this.logUtility = logUtility;
		this.paymentRequestRepository = paymentRequestRepository;
		this.batchDetailRepository = batchDetailRepository;
	}

	@Override
	public PaymentRequestTotalDto getPaymentCountAndAmountByStatus(String correlationId) {
		if (!StringUtils.hasText(correlationId)) {
			throw new BadRequestException(CORRELATION_ID_REQUIRED_ERROR);
		}
		this.logUtility.logMessage("Fetching payment details for correlationId: " + correlationId, "INFO", log);
		List<PaymentRequestTotalDto.PaymentRequest> payments = this.paymentRequestRepository
			.findPaymentRequestsByCorrelationId(correlationId);
		if (payments.isEmpty()) {
			throw new NotFoundException(PAYMENT_REQUEST_NOT_FOUND_BY_CORRELATION_ID_ERROR + correlationId);
		}

		PaymentRequestTotalDto paymentRequestTotalDto = new PaymentRequestTotalDto();
		paymentRequestTotalDto.setTotalPaymentCount(String.valueOf(payments.size()));
		paymentRequestTotalDto.setPaymentRequest(payments);

		return paymentRequestTotalDto;
	}

	@Override
	public List<BatchDetail> getBatchDetailsByCorrelationId(String correlationId) {
		if (!StringUtils.hasText(correlationId)) {
			throw new BadRequestException(CORRELATION_ID_REQUIRED_ERROR);
		}
		this.logUtility.logMessage("Fetching batch details for correlationId: " + correlationId, "INFO", log);
		List<BatchDetail> batchDetails = this.batchDetailRepository.findByCorrelationId(correlationId);
		if (batchDetails.isEmpty()) {
			throw new NotFoundException(BATCH_DETAIL_NOT_FOUND_BY_CORRELATION_ID_ERROR + correlationId);
		}
		return batchDetails;
	}

}
