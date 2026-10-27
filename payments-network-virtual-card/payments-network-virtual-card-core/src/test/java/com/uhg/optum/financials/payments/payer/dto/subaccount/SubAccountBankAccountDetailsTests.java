package com.uhg.optum.financials.payments.payer.dto.subaccount;

import com.uhg.optum.financials.payments.payer.model.SubAccount;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import static org.assertj.core.api.BDDAssertions.then;

class SubAccountBankAccountDetailsTests {

	private SubAccountBankAccountDetails subAccountBankAccountDetails;

	@Test
	@DisplayName("should set and get all fields using setters and this keyword")
	void shouldSetAndGetAllFields() {
		// Arrange
		SubAccount subAccount = new SubAccount();
		String bankAccountNumber = "123456789";
		String bankRoutingNumber = "987654321";
		String payer835Id = "PAYER835ID";
		this.subAccountBankAccountDetails = new SubAccountBankAccountDetails();

		// Act
		this.subAccountBankAccountDetails.setSubAccount(subAccount);
		this.subAccountBankAccountDetails.setBankAccountNumber(bankAccountNumber);
		this.subAccountBankAccountDetails.setBankRoutingNumber(bankRoutingNumber);
		this.subAccountBankAccountDetails.setPayer835Id(payer835Id);

		// Assert
		then(this.subAccountBankAccountDetails.getSubAccount()).isEqualTo(subAccount);
		then(this.subAccountBankAccountDetails.getBankAccountNumber()).isEqualTo(bankAccountNumber);
		then(this.subAccountBankAccountDetails.getBankRoutingNumber()).isEqualTo(bankRoutingNumber);
		then(this.subAccountBankAccountDetails.getPayer835Id()).isEqualTo(payer835Id);
	}

	@Test
	@DisplayName("should set all fields using all-args constructor and this keyword")
	void shouldSetAllFieldsUsingConstructor() {
		// Arrange
		SubAccount subAccount = new SubAccount();
		String bankAccountNumber = "987654321";
		String bankRoutingNumber = "123456789";
		String payer835Id = "PAYERID";
		this.subAccountBankAccountDetails = new SubAccountBankAccountDetails(subAccount, bankAccountNumber,
				bankRoutingNumber, payer835Id);

		// Assert
		then(this.subAccountBankAccountDetails.getSubAccount()).isEqualTo(subAccount);
		then(this.subAccountBankAccountDetails.getBankAccountNumber()).isEqualTo(bankAccountNumber);
		then(this.subAccountBankAccountDetails.getBankRoutingNumber()).isEqualTo(bankRoutingNumber);
		then(this.subAccountBankAccountDetails.getPayer835Id()).isEqualTo(payer835Id);
	}

}
