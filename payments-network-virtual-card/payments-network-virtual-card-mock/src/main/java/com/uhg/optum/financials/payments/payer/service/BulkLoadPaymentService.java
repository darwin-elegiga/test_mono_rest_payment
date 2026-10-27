package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.BulkLoadPaymentRequestDto;
import com.uhg.optum.financials.payments.payer.dto.BulkLoadPaymentResponseDto;

public interface BulkLoadPaymentService {

	BulkLoadPaymentResponseDto bulkLoadPayments(BulkLoadPaymentRequestDto request);

}
