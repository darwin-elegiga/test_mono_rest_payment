package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.PaymentRequestTotalDto;
import com.uhg.optum.financials.payments.payer.model.BatchDetail;

import java.util.List;

public interface BatchDetailService {

	PaymentRequestTotalDto getPaymentCountAndAmountByStatus(String correlationId);

	List<BatchDetail> getBatchDetailsByCorrelationId(String correlationId);

}
