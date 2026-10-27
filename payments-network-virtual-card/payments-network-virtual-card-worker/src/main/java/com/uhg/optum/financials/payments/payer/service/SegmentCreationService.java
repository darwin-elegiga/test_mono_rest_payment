package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountBankAccountDetails;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;

import java.time.LocalDate;

public interface SegmentCreationService {

	String createSegment(PaymentRequestDto paymentRequestDto, SubAccountBankAccountDetails subPayerBankAccount,
			LocalDate date, String networkPaymentId);

}
