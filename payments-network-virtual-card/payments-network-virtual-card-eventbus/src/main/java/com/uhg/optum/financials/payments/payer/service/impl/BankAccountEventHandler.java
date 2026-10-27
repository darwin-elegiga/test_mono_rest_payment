package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.bankaccount.enums.VerificationStatus;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountChanged;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountClosed;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountCreated;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountUnverified;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountVerified;
import com.uhg.optum.financials.payments.payer.constant.ValidationConstants;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.EncryptedBankAccountDto;
import com.uhg.optum.financials.payments.payer.exception.BankAccountException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.events.BankAccount;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.repository.BankAccountRepository;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.service.BaseBankAccountService;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.service.IEventBusHandler;
import com.uhg.optum.financials.payments.payer.util.Utility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.client.events.IEvent;
import com.uhg.optum.financials.shared.core.encryption.EncryptedData;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.UUID;

import static com.uhg.optum.financials.payments.bankaccount.events.BankAccountEventNames.BANK_ACCOUNT_CREATED;
import static com.uhg.optum.financials.payments.bankaccount.events.BankAccountEventNames.BANK_ACCOUNT_UNVERIFIED;
import static com.uhg.optum.financials.payments.bankaccount.events.BankAccountEventNames.BANK_ACCOUNT_VERIFIED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BANK_ACCOUNT_NOT_FOUND_LOG;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ORG_NOT_FOUND_ID_ERROR_LOGS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.WARN;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;

@Slf4j
@Service("bankAccountEventHandler")
public class BankAccountEventHandler extends BaseBankAccountService implements IEventBusHandler {

	public static final String ACCOUNT_NOT_FOUND = "Bank account not found";

	@Value("${endpoint.encrypted.bank-account}")
	private String encryptedBankAccountEndpoint;

	@Autowired
	private BankAccountRepository bankAccountRepository;

	@Autowired
	private OrganizationRepository organizationRepository;

	@Autowired
	private EncryptionAtRestService encryptionAtRestService;

	@Override
	@SuppressWarnings({ "unchecked" })
	public <T extends IEvent> void handle(EventBusEvent<T> event) {
		if (event.getPayload() instanceof BankAccountCreated) {
			handleBankAccountCreated((EventBusEvent<BankAccountCreated>) event);
		}
		else if (event.getPayload() instanceof BankAccountVerified) {
			handleBankAccountVerified((EventBusEvent<BankAccountVerified>) event);
		}
		else if (event.getPayload() instanceof BankAccountChanged) {
			handleBankAccountChanged((EventBusEvent<BankAccountChanged>) event);
		}
		else if (event.getPayload() instanceof BankAccountUnverified) {
			handleBankAccountUnverified((EventBusEvent<BankAccountUnverified>) event);
		}
		else if (event.getPayload() instanceof BankAccountClosed) {
			handleBankAccountClosed((EventBusEvent<BankAccountClosed>) event);
		}
	}

	private void handleBankAccountCreated(EventBusEvent<BankAccountCreated> event) {
		BankAccountCreated bankAccountCreated = event.getPayload();
		UUID bankAccountId = UUID.fromString(event.getId());
		boolean bankAccountExists = this.bankAccountRepository.existsByBankAccountId(bankAccountId);

		if (bankAccountExists) {
			this.logUtility.logMessage("Bank account already exists with the Id: " + event.getId(), WARN, log);
			return;
		}

		String orgShortCode = this.organizationRepository
			.findOrgShortCodeByOrgId(bankAccountCreated.getOrganizationId());

		if (orgShortCode == null) {
			this.logUtility.logMessageWithPriority(
					"Failed to consume bank_account_created event as organisation doesn't exist for bankAccountId: "
							+ event.getId(),
					ERROR, log, P3);
			return;
		}

		String bankAccountNumber = getBankAccountById(this.encryptedBankAccountEndpoint, bankAccountId, orgShortCode,
				EncryptedBankAccountDto.class)
			.getAccountNumber();
		EncryptedData encryptedData = this.encryptionAtRestService.encrypt(bankAccountNumber);

		if (encryptedData == null) {
			this.logUtility.logMessage("Error encrypting account number at rest for Bank Account Id: " + bankAccountId,
					ERROR, log);
			throw new BankAccountException(
					"Error encrypting account number at rest for Bank Account Id: " + bankAccountId);
		}
		// save the bank account details
		BankAccount bankAccount = new BankAccount();
		bankAccount.setBankAccountId(UUID.fromString(event.getId()));
		bankAccount.setName(bankAccountCreated.getName());
		bankAccount.setDisplayName(bankAccountCreated.getDisplayName());
		bankAccount.setOrgId(bankAccountCreated.getOrganizationId());
		bankAccount.setAccountNumberLastFourDigits(bankAccountCreated.getAccountNumberLastFourDigits());
		bankAccount.setAccountNumber(encryptedData.getCipherText());
		bankAccount.setEncryptionAtRestVersion(encryptedData.getKeyVersion());
		bankAccount.setRoutingNumber(bankAccountCreated.getRoutingNumber());
		bankAccount.setAccountType(bankAccountCreated.getAccountType().name());
		bankAccount.setVerificationStatus(bankAccountCreated.getVerificationStatus().name());
		bankAccount.setCreatedOn(LocalDateTime.now());
		bankAccount.setOrgShortCode(orgShortCode);
		bankAccount.setType(BANK_ACCOUNT_CREATED);
		bankAccount.setCreatedBy(VIRTUAL_CARD);
		this.bankAccountRepository.save(bankAccount);
	}

	private void handleBankAccountVerified(EventBusEvent<BankAccountVerified> event) {
		UUID bankAccountId = UUID.fromString(event.getId());
		BankAccount bankAccountDetails = this.bankAccountRepository.findByBankAccountId(bankAccountId);
		if (bankAccountDetails == null) {
			throw new BankAccountException(BANK_ACCOUNT_NOT_FOUND_LOG);
		}
		if (bankAccountDetails.getVerificationStatus().trim().equalsIgnoreCase("Verified")) {
			throw new BankAccountException("Bank account already verified");
		}
		bankAccountDetails.setVerificationStatus(VerificationStatus.VERIFIED.toString());
		bankAccountDetails.setUpdatedOn(LocalDateTime.now());
		bankAccountDetails.setType(BANK_ACCOUNT_VERIFIED);
		this.bankAccountRepository.save(bankAccountDetails);
		this.logUtility.logMessage("Bank account verified successfully for id: " + bankAccountId, INFO, log);

	}

	private void handleBankAccountChanged(EventBusEvent<BankAccountChanged> event) {
		UUID bankAccountId = UUID.fromString(event.getId());
		BankAccountChanged bankAccountChanged = event.getPayload();
		this.saveOrUpdateBankAccountDetails(bankAccountChanged, bankAccountId);
		this.logUtility.logMessage("Bank account details updated successfully for id: " + bankAccountId, INFO, log);
	}

	private void handleBankAccountUnverified(EventBusEvent<BankAccountUnverified> event) {

		UUID bankAccountId = UUID.fromString(event.getId());
		BankAccount bankAccountDetails = this.bankAccountRepository.findByBankAccountId(bankAccountId);
		if (bankAccountDetails == null) {
			throw new BankAccountException(ACCOUNT_NOT_FOUND);
		}
		if (bankAccountDetails.getVerificationStatus().trim().equalsIgnoreCase("NotVerified")) {
			throw new BankAccountException("Bank account already unverified");
		}
		bankAccountDetails.setVerificationStatus(VerificationStatus.NOT_VERIFIED.toString());
		bankAccountDetails.setUpdatedOn(LocalDateTime.now());
		bankAccountDetails.setType(BANK_ACCOUNT_UNVERIFIED);
		bankAccountDetails.setUpdatedBy(VIRTUAL_CARD);
		this.bankAccountRepository.save(bankAccountDetails);
		this.logUtility.logMessage("Bank account unverified successfully for id: " + bankAccountId, INFO, log);
	}

	private void handleBankAccountClosed(EventBusEvent<BankAccountClosed> event) {
		UUID bankAccountId = UUID.fromString(event.getId());
		BankAccount bankAccountDetails = this.bankAccountRepository.findByBankAccountId(bankAccountId);
		if (bankAccountDetails == null) {
			throw new BankAccountException(BANK_ACCOUNT_NOT_FOUND_LOG);
		}
		if (bankAccountDetails.getVerificationStatus().trim().equalsIgnoreCase("CLOSED")) {
			throw new BankAccountException("Bank account already closed");
		}
		bankAccountDetails.setVerificationStatus(VerificationStatus.CLOSED.toString());
		bankAccountDetails.setUpdatedBy(VIRTUAL_CARD);
		bankAccountDetails.setType("bank-account.closed");
		this.bankAccountRepository.save(bankAccountDetails);
		this.logUtility.logMessage("Bank account closed successfully for id: " + bankAccountId, INFO, log);
	}

	public void saveOrUpdateBankAccountDetails(BankAccountChanged bankAccountChanged, UUID bankAccountId) {
		BankAccount bankAccountDetails = this.bankAccountRepository.findByBankAccountId(bankAccountId);
		if (bankAccountDetails == null) {
			String orgShortCode = this.getOrgShortCodeElseRaiseP3(bankAccountChanged.getOrganizationId());
			EncryptedData encryptedData = this.getEncryptedBankAccount(orgShortCode, bankAccountId);

			bankAccountDetails = new BankAccount();
			bankAccountDetails.setOrgShortCode(orgShortCode);
			bankAccountDetails.setBankAccountId(bankAccountId);
			bankAccountDetails.setCreatedBy(VIRTUAL_CARD);
			bankAccountDetails.setCreatedOn(LocalDateTime.now());
			bankAccountDetails.setAccountNumber(encryptedData.getCipherText());
			bankAccountDetails.setEncryptionAtRestVersion(encryptedData.getKeyVersion());

		}
		else {
			bankAccountDetails.setUpdatedOn(LocalDateTime.now());
			bankAccountDetails.setUpdatedBy(VIRTUAL_CARD);
		}
		this.updateEntity(bankAccountChanged, bankAccountDetails);

		this.bankAccountRepository.save(bankAccountDetails);
		this.logUtility.logMessage("Bank account details saved/updated successfully for id: " + bankAccountId,
				ValidationConstants.INFO, log);
	}

	private String getOrgShortCodeElseRaiseP3(UUID orgId) {
		Organization organization = this.organizationRepository.findByOrgId(orgId);
		if (organization != null) {
			return organization.getOrgShortCode();
		}
		else {
			this.logUtility.logMessageWithPriority(ORG_NOT_FOUND_ID_ERROR_LOGS + orgId, ValidationConstants.ERROR, log,
					P3);
			throw new NotFoundException("Organization not found for Id: " + orgId);
		}
	}

	private EncryptedData getEncryptedBankAccount(String orgShortCode, UUID bankAccountId) {
		String bankAccountNumber = getBankAccountById(this.encryptedBankAccountEndpoint, bankAccountId, orgShortCode,
				EncryptedBankAccountDto.class)
			.getAccountNumber();
		EncryptedData encryptedData = this.encryptionAtRestService.encrypt(bankAccountNumber);

		if (encryptedData == null) {
			this.logUtility.logMessage("Error encrypting account number at rest for Bank Account Id: " + bankAccountId,
					ValidationConstants.ERROR, log);
			throw new BankAccountException(
					"Error encrypting account number at rest for Bank Account Id: " + bankAccountId);
		}
		return encryptedData;
	}

	private void updateEntity(BankAccountChanged dto, BankAccount bankAccount) {
		bankAccount.setOrgId(dto.getOrganizationId());
		bankAccount.setName(Utility.getString(dto.getName(), bankAccount.getName()));
		bankAccount.setDisplayName(Utility.getString(dto.getDisplayName(), bankAccount.getDisplayName()));
		bankAccount.setRoutingNumber(Utility.getString(dto.getRoutingNumber(), bankAccount.getRoutingNumber()));
		bankAccount.setAccountNumberLastFourDigits(
				Utility.getString(dto.getAccountNumberLastFourDigits(), bankAccount.getAccountNumberLastFourDigits()));
		bankAccount.setAccountType(Utility.getString(dto.getAccountType(), bankAccount.getAccountType()));
		bankAccount.setType(Utility.getString(dto.getType(), bankAccount.getType()));
		bankAccount
			.setVerificationStatus(Utility.getString(dto.getVerificationStatus(), bankAccount.getVerificationStatus()));
	}

}
