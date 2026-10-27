package com.uhg.optum.financials.payments.payer.dto.batch;

import java.math.BigDecimal;

public interface CorrelatedBatchDetail {

	Long getNumberOfPayments();

	String getCorrelationId();

	BigDecimal getTotalAmount();

}
