package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.account.AccountDto;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.payments.payer.model.events.BankAccount;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.service.AccountsDetailService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;

@ExtendWith(MockitoExtension.class)
public class AccountsDetailControllerTests {

	@InjectMocks
	AccountsDetailController accountsDetailController;

	@Mock
	AccountsDetailService accountsDetailService;

	@Mock
	private LogUtility logUtility;

	@Test
	void testGetAccount() {
		Assertions.assertThat(this.accountsDetailController.getAccount("testCode", "orgId")).isNotNull();
	}

	@Test
	void testGetSubAccount() {
		UUID orgId = UUID.randomUUID();
		Assertions.assertThat(this.accountsDetailController.getSubAccount("testCode", "subtestCode", "orgId"))
			.isNotNull();
	}

	@Test
	void getBankAccountDetails_returnsBankAccountDetailsForValidInputs() {
		UUID organizationId = UUID.randomUUID();
		String routingNumber = "123456789";
		BankAccount bankAccountDetails = new BankAccount();
		bankAccountDetails.setOrgId(organizationId);
		bankAccountDetails.setRoutingNumber(routingNumber);

		Mockito.when(this.accountsDetailService.getBankAccountDetails(organizationId)).thenReturn(bankAccountDetails);

		ResponseEntity<BankAccount> response = this.accountsDetailController.getBankAccountDetails(organizationId);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(bankAccountDetails);
	}

	@Test
	void getBankAccountDetails_throwsExceptionForInvalidRoutingNumber() {
		UUID organizationId = UUID.randomUUID();
		String routingNumber = "invalid";

		Mockito.when(this.accountsDetailService.getBankAccountDetails(organizationId))
			.thenThrow(new IllegalArgumentException("Invalid routing number"));

		Assertions.assertThatThrownBy(() -> this.accountsDetailController.getBankAccountDetails(organizationId))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid routing number");
	}

	@Test
	void testGetOrganisation() {
		Assertions.assertThat(this.accountsDetailController.getOrganization("test")).isNotNull();
	}

	@Test
	void getAccount_returnsAccountDetailsForValidInputs() {
		String orgShortCode = "orgId";
		String accountShortCode = "testCode";
		AccountDto accountDto = new AccountDto();
		given(this.accountsDetailService.getAccount(eq(accountShortCode), eq(orgShortCode))).willReturn(accountDto);

		ResponseEntity<AccountDto> response = this.accountsDetailController.getAccount(orgShortCode, accountShortCode);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(accountDto);
		then(this.logUtility).should().logMessage(any(String.class), any(String.class), any(org.slf4j.Logger.class));
	}

	@Test
	void getAccount_throwsExceptionForInvalidAccount() {
		String orgShortCode = "orgId";
		String accountShortCode = "invalid";
		given(this.accountsDetailService.getAccount(eq(accountShortCode), eq(orgShortCode)))
			.willThrow(new IllegalArgumentException("Invalid account"));

		Assertions.assertThatThrownBy(() -> this.accountsDetailController.getAccount(orgShortCode, accountShortCode))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid account");
	}

	@Test
	void getSubAccount_returnsSubAccountDetailsForValidInputs() {
		String orgShortCode = "orgId";
		String accountShortCode = "testCode";
		String subAccountShortCode = "subtestCode";
		SubAccountDto subAccountDto = new SubAccountDto();
		given(this.accountsDetailService.getSubAccount(eq(accountShortCode), eq(subAccountShortCode), eq(orgShortCode)))
			.willReturn(subAccountDto);

		ResponseEntity<SubAccountDto> response = this.accountsDetailController.getSubAccount(orgShortCode,
				accountShortCode, subAccountShortCode);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(subAccountDto);
		then(this.logUtility).should().logMessage(any(String.class), any(String.class), any(org.slf4j.Logger.class));
	}

	@Test
	void getSubAccount_throwsExceptionForInvalidSubAccount() {
		String orgShortCode = "orgId";
		String accountShortCode = "testCode";
		String subAccountShortCode = "invalid";
		given(this.accountsDetailService.getSubAccount(eq(accountShortCode), eq(subAccountShortCode), eq(orgShortCode)))
			.willThrow(new IllegalArgumentException("Invalid sub account"));

		Assertions.assertThatThrownBy(
				() -> this.accountsDetailController.getSubAccount(orgShortCode, accountShortCode, subAccountShortCode))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid sub account");
	}

	@Test
	void getOrganization_returnsOrganizationDetailsForValidInputs() {
		String orgShortCode = "orgId";
		Organization organization = new Organization();
		given(this.accountsDetailService.getOrganizationRegisteredDetails(eq(orgShortCode))).willReturn(organization);

		ResponseEntity<Organization> response = this.accountsDetailController.getOrganization(orgShortCode);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(organization);
		then(this.logUtility).should().logMessage(any(String.class), any(String.class), any(org.slf4j.Logger.class));
	}

	@Test
	void getOrganization_throwsExceptionForInvalidOrganization() {
		String orgShortCode = "invalid";
		given(this.accountsDetailService.getOrganizationRegisteredDetails(eq(orgShortCode)))
			.willThrow(new IllegalArgumentException("Invalid organization"));

		Assertions.assertThatThrownBy(() -> this.accountsDetailController.getOrganization(orgShortCode))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid organization");
	}

	@Test
	void getBankAccountDetails_logsAndReturnsBankAccountDetails() {
		UUID bankAccountId = UUID.randomUUID();
		BankAccount bankAccount = new BankAccount();
		given(this.accountsDetailService.getBankAccountDetails(eq(bankAccountId))).willReturn(bankAccount);

		ResponseEntity<BankAccount> response = this.accountsDetailController.getBankAccountDetails(bankAccountId);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(bankAccount);
		then(this.logUtility).should().logMessage(any(String.class), any(String.class), any(org.slf4j.Logger.class));
	}

	@Test
	void getBankAccountDetails_throwsExceptionForInvalidBankAccountId() {
		UUID bankAccountId = UUID.randomUUID();
		given(this.accountsDetailService.getBankAccountDetails(eq(bankAccountId)))
			.willThrow(new IllegalArgumentException("Invalid bank account"));

		Assertions.assertThatThrownBy(() -> this.accountsDetailController.getBankAccountDetails(bankAccountId))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid bank account");
	}

}
