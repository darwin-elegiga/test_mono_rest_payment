package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.constant.PaymentGroupingLevelType;
import com.uhg.optum.financials.payments.payer.model.PaymentGroupingLevel;

public interface PaymentGroupingLevelService {

	PaymentGroupingLevel savePaymentGroupingLevel(String orgShortCode,
			PaymentGroupingLevelType paymentGroupingLevelType);

	PaymentGroupingLevel getPaymentGroupLevel(String orgShortCode);

}
