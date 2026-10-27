package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountBankAccountDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@NoArgsConstructor
public class PaymentRequestWrapper {

	@Getter
	@Setter
	private PaymentRequest paymentRequest;

	@Getter
	@Setter
	private SubAccountBankAccountDetails subAccountBankAccountDetails;

}
