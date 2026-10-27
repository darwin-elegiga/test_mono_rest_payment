package com.uhg.optum.financials.payments.payer.dto.bankaccount;

import lombok.Getter;
import lombok.Setter;

public class EncryptedLocalBankAccountDto extends BaseEncryptedBankAccountDto {

	@Getter
	@Setter
	private String achCompanyId;

}
