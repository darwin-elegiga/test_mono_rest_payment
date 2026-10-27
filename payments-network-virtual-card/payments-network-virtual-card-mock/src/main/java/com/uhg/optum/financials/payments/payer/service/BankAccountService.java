package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.bankaccount.BankAccountEventDto;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.BaseEncryptedBankAccountDto;

import java.util.UUID;

public interface BankAccountService {

	String publishEvent(BankAccountEventDto bankAccount);

	BaseEncryptedBankAccountDto getEncryptedBankAccountById(UUID id, String originPublicKeyAsBase64, String tenantId);

}
