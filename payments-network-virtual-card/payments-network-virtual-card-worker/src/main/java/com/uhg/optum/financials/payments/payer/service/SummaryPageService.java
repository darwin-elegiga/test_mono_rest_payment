package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.model.PaymentRequest;

import java.time.LocalDate;
import java.util.UUID;

public interface SummaryPageService {

	void processSummaryDocumentMessage(PaymentRequest paymentRequest, LocalDate expiryDate, UUID cardGuid)
			throws Exception;

}
