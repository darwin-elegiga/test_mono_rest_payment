package com.uhg.optum.financials.payments.payer.dto.subaccount;

import com.uhg.optum.financials.payments.payer.model.SubAccount;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@NoArgsConstructor
public class SubAccountBankAccountDetails {

	@Getter
	@Setter
	private String payer835Id;

	@Getter
	@Setter
	private SubAccount subAccount;

	@Getter
	@Setter
	private String bankAccountNumber;

	@Getter
	@Setter
	private String bankRoutingNumber;

	public SubAccountBankAccountDetails(SubAccount subAccount, String bankAccountNumber, String bankRoutingNumber,
			String payer835Id) {
		this.subAccount = subAccount;
		this.bankAccountNumber = bankAccountNumber;
		this.bankRoutingNumber = bankRoutingNumber;
		this.payer835Id = payer835Id;
	}

}
