package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.account.AccountDto;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.payments.payer.model.events.BankAccount;
import com.uhg.optum.financials.payments.payer.model.events.Organization;

import java.util.UUID;

public interface AccountsDetailService {

	BankAccount getBankAccountDetails(UUID bankAccountId);

	AccountDto getAccount(String accountShortCode, String orgShortCode);

	SubAccountDto getSubAccount(String accountShortCode, String subAccountShortCode, String orgShortCode);

	Organization getOrganizationRegisteredDetails(String orgShortCode);

}
