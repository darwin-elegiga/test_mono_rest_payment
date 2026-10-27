package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.bankaccount.enums.AccountType;
import com.uhg.optum.financials.payments.bankaccount.enums.VerificationStatus;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountCreated;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.BankAccountEventDto;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.BaseEncryptedBankAccountDto;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.EncryptedBankAccountDto;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.EncryptedLocalBankAccountDto;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.service.BankAccountService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import lombok.NoArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.bouncycastle.crypto.AsymmetricCipherKeyPair;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Service;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.security.SecureRandom;
import java.time.Instant;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

import static com.uhg.optum.financials.payments.bankaccount.events.BankAccountEventNames.BANK_ACCOUNT_CREATED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.FAILED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.SEND_MESSAGE_EVENTBUS_LOG;
import static com.uhg.optum.financials.payments.payer.constant.Constants.SEND_MESSAGE_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.Constants.SUCCESS_EVENT;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@Service
@Slf4j
@NoArgsConstructor
public class BankAccountServiceImpl implements BankAccountService {

	private Map<String, String> keyMap;

	public static final String LOCAL_PUBLIC = "local_public";

	public static final String LOCAL_PRIVATE = "local_private";

	@Autowired
	@Qualifier("kafkaEventBusProducer")
	private EventBusProducer producerService;

	@Autowired
	private LogUtility logUtility;

	@Autowired
	private AssymetricEncryptionService eccEncryptionService;

	@EventListener(ApplicationReadyEvent.class)
	public void init() {
		this.keyMap = new HashMap<>();
		try {
			generateKeysIfAbsent();
		}
		catch (GeneralSecurityException | IOException ex) {
			log.error("Error occurred while generating keys", ex);
		}
	}

	@Override
	public String publishEvent(BankAccountEventDto bankAccountEventDto) {
		try {
			this.logUtility.logMessage(SEND_MESSAGE_EVENTBUS_LOG, INFO, log);
			EventBusEvent<BankAccountCreated> event = convertBankAccountDetailsDtoToEventMessage(bankAccountEventDto);
			this.producerService.sendEvent(event, null);
			return SUCCESS_EVENT;
		}
		catch (Exception ex) {
			this.logUtility.logMessage(SEND_MESSAGE_EXCEPTION_LOG + ex.getMessage(), ERROR, log);
			return FAILED;
		}
	}

	@Override
	public BaseEncryptedBankAccountDto getEncryptedBankAccountById(UUID id, String publicKeyAsBase64, String tenantId) {
		try {
			String localPrivateKey = this.keyMap.get(LOCAL_PRIVATE);

			byte[] decodedBytes = Base64.getDecoder().decode(publicKeyAsBase64);
			String remotePublicKey = new String(decodedBytes);

			SecureRandom secureRandom = new SecureRandom();

			BaseEncryptedBankAccountDto encryptedBankAccountDto;
			if (tenantId == null) {
				String randomAchCompanyId = String.format("%010d", secureRandom.nextInt(1_000_000_000));
				encryptedBankAccountDto = new EncryptedLocalBankAccountDto();
				((EncryptedLocalBankAccountDto) encryptedBankAccountDto).setAchCompanyId(randomAchCompanyId);
				((EncryptedLocalBankAccountDto) encryptedBankAccountDto)
					.setVerificationStatus(VerificationStatus.VERIFIED);
			}
			else {
				encryptedBankAccountDto = new EncryptedBankAccountDto();
				((EncryptedBankAccountDto) encryptedBankAccountDto).setVerificationStatus(VerificationStatus.VERIFIED);
			}

			String randomAccountNumber = String
				.valueOf(Math.abs(secureRandom.nextLong() % 9_000_000_000_000_000L + 1_000_000_000_000_000L));
			log.info("Random account number generated: {}", randomAccountNumber);
			String encryptedAccountNumber = this.eccEncryptionService.encrypt(randomAccountNumber, localPrivateKey,
					remotePublicKey);
			log.info("Encrypted account number: {}", encryptedAccountNumber);
			encryptedBankAccountDto.setEncryptedAccountNumber(encryptedAccountNumber);
			encryptedBankAccountDto.setRoutingNumber(String.valueOf(secureRandom.nextInt(900_000_000) + 100_000_000));
			encryptedBankAccountDto.setAccountType(AccountType.CHECKING);

			String localPublicKey = this.keyMap.get(LOCAL_PUBLIC);
			String localPublicKeyAsBase64 = Base64.getEncoder().encodeToString(localPublicKey.getBytes());

			encryptedBankAccountDto.setPublicKeyAsBase64(localPublicKeyAsBase64);

			return encryptedBankAccountDto;
		}
		catch (GeneralSecurityException | IOException ex) {
			log.error("Exception occurred while encrypting bank account details", ex);
			throw new InternalServerException("Exception occurred while encrypting bank account details");
		}
	}

	private void generateKeysIfAbsent() throws GeneralSecurityException, IOException {
		String localPublicKey = this.keyMap.get(LOCAL_PUBLIC);
		if (StringUtils.isEmpty(localPublicKey)) {
			log.info("Generating ECC keys");
			AsymmetricCipherKeyPair keys = this.eccEncryptionService.generateKeyPair();

			localPublicKey = this.eccEncryptionService.getPublicKeyInPemFormat(keys.getPublic());
			String localPrivateKey = this.eccEncryptionService.getPrivateKeyInPemFormat(keys.getPrivate());
			this.keyMap.put(LOCAL_PUBLIC, localPublicKey);
			this.keyMap.put(LOCAL_PRIVATE, localPrivateKey);
		}
	}

	private EventBusEvent<BankAccountCreated> convertBankAccountDetailsDtoToEventMessage(
			BankAccountEventDto bankAccountEvent) {
		EventBusEvent<BankAccountCreated> eventMessage = new EventBusEvent<>();
		eventMessage.setId(String.valueOf(bankAccountEvent.getId()));
		eventMessage.setTenantId(String.valueOf(bankAccountEvent.getTenantId()));
		eventMessage.setEventId(bankAccountEvent.getEventId());
		eventMessage.setPayload(bankAccountEvent.getBankAccountCreated());
		eventMessage.setType(BANK_ACCOUNT_CREATED);
		eventMessage.setCreatedOn(Instant.now());
		return eventMessage;
	}

}
