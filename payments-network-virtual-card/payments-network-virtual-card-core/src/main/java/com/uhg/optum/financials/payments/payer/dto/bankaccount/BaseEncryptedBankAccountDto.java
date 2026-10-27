package com.uhg.optum.financials.payments.payer.dto.bankaccount;

import com.uhg.optum.financials.payments.bankaccount.enums.AccountType;
import com.uhg.optum.financials.payments.bankaccount.enums.VerificationStatus;
import lombok.Getter;
import lombok.Setter;

public class BaseEncryptedBankAccountDto {

	@Getter
	@Setter
	private String routingNumber;

	@Getter
	@Setter
	private String encryptedAccountNumber;

	@Getter
	@Setter
	private AccountType accountType;

	@Getter
	@Setter
	private String publicKeyAsBase64;

	@Getter
	@Setter
	private String accountNumber;

	@Getter
	@Setter
	private VerificationStatus verificationStatus;

}
