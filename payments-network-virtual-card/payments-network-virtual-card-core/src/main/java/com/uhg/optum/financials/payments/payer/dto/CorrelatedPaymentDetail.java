package com.uhg.optum.financials.payments.payer.dto;

import java.math.BigDecimal;

public interface CorrelatedPaymentDetail {

	String getCorrelationId();

	Long getNumberOfPayments();

	BigDecimal getTotalAmount();

}
