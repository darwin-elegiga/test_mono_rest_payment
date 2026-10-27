package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.enums.FundingMethod;
import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.models.Address;
import com.uhg.optum.financials.payments.payer.exception.BankAccountException;
import com.uhg.optum.financials.payments.payer.mapper.AccountMapper;
import com.uhg.optum.financials.payments.payer.mapper.SubAccountMapper;
import com.uhg.optum.financials.payments.payer.model.Account;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.model.events.BankAccount;
import com.uhg.optum.financials.payments.payer.repository.AccountRepository;
import com.uhg.optum.financials.payments.payer.repository.BankAccountRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Optional;
import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class AccountsDetailServiceImplTests {

	@InjectMocks
	private AccountsDetailServiceImpl accountsDetailServiceImpl;

	@Mock
	private AccountRepository accountRepository;

	@Mock
	private SubAccountRepository subAccountRepository;

	@Mock
	private BankAccountRepository bankAccountDetailsRepository;

	@Mock
	private AccountMapper accountMapper;

	@Mock
	private SubAccountMapper subAccountMapper;

	UUID orgId = UUID.fromString("d2e88480-4c83-4f5f-b7b8-01a52e328b6d");

	@Test
	void testGetAccount() {
		Mockito.when(this.accountRepository.findByAccountShortCodeAndOrgShortCode(any(), any())).thenReturn(getPayer());
		Assertions.assertThat(this.accountsDetailServiceImpl.getAccount("testCode", "testCode")).isNotNull();

	}

	@Test
	void testGetAccountNull() {
		Assertions.assertThatException()
			.isThrownBy(() -> this.accountsDetailServiceImpl.getAccount("testCode", "testCode"));
	}

	@Test
	void testGetSubPayer() {

		Mockito
			.when(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(any(), any(),
					any()))
			.thenReturn(Optional.ofNullable(getSubPayer()));
		Assertions.assertThat(this.accountsDetailServiceImpl.getSubAccount("testCode", "testCode", "test")).isNotNull();
	}

	@Test
	void testGetSubPayerNull() {
		Mockito
			.when(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(any(), any(),
					any()))
			.thenReturn(null);
		Assertions.assertThatException()
			.isThrownBy(() -> this.accountsDetailServiceImpl.getSubAccount("test", "test", "test"));
	}

	@Test
	public void getBankAccountDetails_returnsBankAccountDetailsWhenFound() {
		String routingNumber = "123456789";
		BankAccount bankAccountDetails = new BankAccount();
		bankAccountDetails.setOrgId(this.orgId);
		bankAccountDetails.setRoutingNumber(routingNumber);

		Mockito.when(this.bankAccountDetailsRepository.findByBankAccountId(this.orgId)).thenReturn(bankAccountDetails);

		Assertions.assertThat(this.accountsDetailServiceImpl.getBankAccountDetails(this.orgId))
			.isEqualTo(bankAccountDetails);
	}

	@Test
	public void getBankAccountDetails_throwsExceptionWhenBankAccountDetailsNotFound() {
		String routingNumber = "123456789";

		Mockito.when(this.bankAccountDetailsRepository.findByBankAccountId(this.orgId)).thenReturn(null);

		Assertions.assertThatExceptionOfType(BankAccountException.class)
			.isThrownBy(() -> this.accountsDetailServiceImpl.getBankAccountDetails(this.orgId))
			.withMessageContaining("Bank Account detail not found for this bankAccountId: " + this.orgId);
	}

	public Address getAddressDto() {
		Address addressDto = new Address();
		addressDto.setAddressLine1("Test Address");
		addressDto.setAddressLine2("Test Address");
		addressDto.setCity("Test City");
		addressDto.setState("Test State");
		addressDto.setPostalCode("123456");
		return addressDto;
	}

	private Account getPayer() {
		UUID accountId = UUID.fromString("7fa85f64-5787-4562-b3fc-2c963f66afa6");
		Account account = new Account();
		account.setAddress(getAddressDto());
		account.setMarketType(MarketType.MEDICAL.toString());
		account.setFundingMethod(FundingMethod.PRE_FUNDED_NEXT_DAY.toString());
		account.setName("Test Payer");
		account.setAccountId(accountId);
		account.setFaxNumber("123456789");
		account.setPhoneNumber("123456789");
		account.setContactEmail("test");
		account.setTin("123456789");
		account.setHoldAndRelease(true);
		account.setFundingMethod(null);
		account.setProgramId("123456789");
		account.setAccountId(UUID.randomUUID());
		return account;
	}

	public SubAccount getSubPayer() {
		UUID subAccountId = UUID.fromString("7fa85f64-5787-4562-b3fc-2c963f66afa6");
		SubAccount subAccount = new SubAccount();
		subAccount.setAddress(getAddressDto());
		subAccount.setMarketType(MarketType.MEDICAL.toString());
		subAccount.setFundingMethod(FundingMethod.PRE_FUNDED_NEXT_DAY.toString());
		subAccount.setName("Test Payer");
		subAccount.setSubAccountId(subAccountId);
		subAccount.setFaxNumber("123456789");
		subAccount.setPhoneNumber("123456789");
		subAccount.setContactEmail("test");
		subAccount.setTin("123456789");
		subAccount.setHoldAndRelease(true);
		subAccount.setFundingMethod(null);
		subAccount.setProgramId("123456789");
		subAccount.setAccountShortCode("code");
		return subAccount;
	}

}
