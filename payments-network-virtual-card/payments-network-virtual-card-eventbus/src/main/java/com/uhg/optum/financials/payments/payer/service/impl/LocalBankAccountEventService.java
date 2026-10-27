package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.bankaccount.events.LocalBankAccountCreated;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.EncryptedLocalBankAccountDto;
import com.uhg.optum.financials.payments.payer.exception.BankAccountException;
import com.uhg.optum.financials.payments.payer.model.events.LocalBankAccount;
import com.uhg.optum.financials.payments.payer.repository.LocalBankAccountRepository;
import com.uhg.optum.financials.payments.payer.service.BaseBankAccountService;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.service.IEventBusHandler;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.client.events.IEvent;
import com.uhg.optum.financials.shared.core.encryption.EncryptedData;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.UUID;

import static com.uhg.optum.financials.payments.bankaccount.events.LocalBankAccountEventNames.LOCAL_BANK_ACCOUNT_CREATED;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;

@Slf4j
@Service("localBankAccountEventHandler")
public class LocalBankAccountEventService extends BaseBankAccountService implements IEventBusHandler {

	@Value("${endpoint.encrypted.local-bank-account}")
	private String encryptedLocalBankAccountEndpoint;

	@Autowired
	private LocalBankAccountRepository localBankAccountRepository;

	@Autowired
	private EncryptionAtRestService encryptionAtRestService;

	@Override
	public <T extends IEvent> void handle(EventBusEvent<T> event) {
		if (event.getPayload() instanceof LocalBankAccountCreated) {
			handleLocalBankAccountCreated((EventBusEvent<LocalBankAccountCreated>) event);
		}
	}

	private void handleLocalBankAccountCreated(EventBusEvent<LocalBankAccountCreated> event) {
		LocalBankAccount localBankAccount = this.localBankAccountRepository
			.findByLocalBankAccountId(UUID.fromString(event.getId()));

		if (localBankAccount != null) {
			this.logUtility.logMessage("Local Bank Account already exists with the Id: " + event.getId(), ERROR, log);
			return;
		}
		localBankAccount = new LocalBankAccount();
		UUID localBankAccountId = UUID.fromString(event.getId());
		LocalBankAccountCreated localBankAccountCreated = event.getPayload();
		localBankAccount.setLocalBankAccountId(localBankAccountId);
		localBankAccount.setAccountName(localBankAccountCreated.getName());
		localBankAccount.setRoutingNumber(localBankAccountCreated.getRoutingNumber());

		String accountNumber = getBankAccountById(this.encryptedLocalBankAccountEndpoint, localBankAccountId, null,
				EncryptedLocalBankAccountDto.class)
			.getAccountNumber();

		EncryptedData encryptedData = this.encryptionAtRestService.encrypt(accountNumber);

		if (encryptedData == null) {
			this.logUtility.logMessage(
					"Error encrypting account number at rest for Local Bank Account Id: " + localBankAccountId, ERROR,
					log);
			throw new BankAccountException(
					"Error encrypting account number at rest for Local Bank Account Id: " + localBankAccountId);
		}

		localBankAccount.setAccountNumber(encryptedData.getCipherText());
		localBankAccount.setEncryptionAtRestVersion(encryptedData.getKeyVersion());
		localBankAccount.setAccountNumberLastFourDigits(localBankAccountCreated.getAccountNumberLastFourDigits());
		localBankAccount.setAccountType(localBankAccountCreated.getAccountType().toString());
		localBankAccount.setVerificationStatus(localBankAccountCreated.getVerificationStatus().toString());
		localBankAccount.setCreatedOn(LocalDateTime.now());
		localBankAccount.setAchCompanyId(localBankAccountCreated.getAchCompanyId());
		localBankAccount.setType(LOCAL_BANK_ACCOUNT_CREATED);
		this.localBankAccountRepository.save(localBankAccount);
		this.logUtility.logMessage("Local Bank account details updated successfully for Id: " + localBankAccountId,
				INFO, log);

	}

}
