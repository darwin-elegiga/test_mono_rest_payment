package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.model.DocumentResponseDetails;

import java.util.List;

public interface DocumentResponseDetailsService {

	List<DocumentResponseDetails> getDocumentResponseDetails(String networkPaymentId);

}
