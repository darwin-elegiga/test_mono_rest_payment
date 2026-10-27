package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.account.AccountDto;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.payments.payer.exception.BankAccountException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.mapper.AccountMapper;
import com.uhg.optum.financials.payments.payer.mapper.SubAccountMapper;
import com.uhg.optum.financials.payments.payer.model.Account;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.model.events.BankAccount;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.repository.AccountRepository;
import com.uhg.optum.financials.payments.payer.repository.BankAccountRepository;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.AccountsDetailService;
import org.springframework.stereotype.Service;

import java.util.Optional;
import java.util.UUID;

@Service
public class AccountsDetailServiceImpl implements AccountsDetailService {

	private BankAccountRepository bankAccountDetailsRepository;

	private AccountRepository accountRepository;

	private SubAccountRepository subAccountRepository;

	private OrganizationRepository organizationRepository;

	public AccountsDetailServiceImpl(BankAccountRepository bankAccountDetailsRepository,
			AccountRepository accountRepository, SubAccountRepository subAccountRepository,
			OrganizationRepository organizationRepository) {
		this.bankAccountDetailsRepository = bankAccountDetailsRepository;
		this.accountRepository = accountRepository;
		this.subAccountRepository = subAccountRepository;
		this.organizationRepository = organizationRepository;
	}

	@Override
	public BankAccount getBankAccountDetails(UUID bankAccountId) {
		BankAccount bankAccountDetails = this.bankAccountDetailsRepository.findByBankAccountId(bankAccountId);
		if (bankAccountDetails != null) {
			return bankAccountDetails;
		}
		else {
			throw new BankAccountException("Bank Account detail not found for this bankAccountId: " + bankAccountId);
		}

	}

	@Override
	public AccountDto getAccount(String accountShortCode, String orgShortCode) {

		Account account = this.accountRepository.findByAccountShortCodeAndOrgShortCode(accountShortCode, orgShortCode);
		if (account != null) {
			AccountDto accountDto = new AccountDto();
			AccountMapper.toDto(account, accountDto);
			return accountDto;
		}

		else {
			throw new NotFoundException("Account detail not found for this orgShortCode: " + orgShortCode
					+ " and accountShortCode: " + accountShortCode + " combination");
		}

	}

	@Override
	public SubAccountDto getSubAccount(String accountShortCode, String subAccountShortCode, String orgShortCode) {

		Optional<SubAccount> subAccount = this.subAccountRepository
			.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(accountShortCode, subAccountShortCode,
					orgShortCode);
		if (subAccount.isPresent()) {
			SubAccountDto subPayerDto = new SubAccountDto();
			SubAccountMapper.toDto(subAccount.get(), subPayerDto);
			return subPayerDto;
		}
		else {
			throw new NotFoundException("sub account details not found for this subAccountShortCode : "
					+ subAccountShortCode + " and accountShortCode: " + accountShortCode + " and orgShortCode : "
					+ orgShortCode + " combination");
		}

	}

	@Override
	public Organization getOrganizationRegisteredDetails(String orgShortCode) {
		Organization organizationRegisteredDetails = this.organizationRepository.findByOrgShortCode(orgShortCode);
		if (organizationRegisteredDetails != null) {
			return organizationRegisteredDetails;
		}
		else {
			throw new NotFoundException("Organization detail not found for this orgShortCode: " + orgShortCode);
		}

	}

}
