package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.bankaccount.events.BankAccountCreated;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.BankAccountEventDto;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.core.encryption.AssymetricEncryptionService;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.mockito.BDDMockito;
import org.mockito.ArgumentMatchers;
import org.bouncycastle.crypto.AsymmetricCipherKeyPair;

import java.util.HashMap;
import java.util.Map;
import java.util.UUID;

import static org.assertj.core.api.BDDAssertions.then;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.willThrow;
import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
class BankAccountServiceImplTests {

	@InjectMocks
	private BankAccountServiceImpl bankAccountServiceImpl;

	@Mock
	private EventBusProducer producerService;

	@Mock
	private LogUtility logUtility;

	@Mock
	private AssymetricEncryptionService eccEncryptionService;

	private BankAccountEventDto bankAccountEventDto;

	private BankAccountCreated bankAccountCreated;

	@BeforeEach
	void setUp() {
		this.bankAccountEventDto = new BankAccountEventDto();
		this.bankAccountEventDto.setId(UUID.randomUUID());
		this.bankAccountEventDto.setTenantId(UUID.randomUUID());
		this.bankAccountEventDto.setEventId(UUID.randomUUID());
		this.bankAccountCreated = new BankAccountCreated();
		this.bankAccountEventDto.setBankAccountCreated(this.bankAccountCreated);
	}

	@Test
	void publishEvent_success() {
		String result = this.bankAccountServiceImpl.publishEvent(this.bankAccountEventDto);
		then(result).isEqualTo("SUCCESS");
		BDDMockito.then(this.producerService).should().sendEvent(any(EventBusEvent.class), ArgumentMatchers.isNull());
		BDDMockito.then(this.logUtility)
			.should()
			.logMessage(ArgumentMatchers.eq("Sending message to event bus"), ArgumentMatchers.eq("info"),
					ArgumentMatchers.any());
	}

	@Test
	void publishEvent_failure() {
		willThrow(new RuntimeException("fail")).given(this.producerService).sendEvent(any(EventBusEvent.class), any());
		String result = this.bankAccountServiceImpl.publishEvent(this.bankAccountEventDto);
		then(result).isEqualTo("Failed");
		BDDMockito.then(this.logUtility)
			.should()
			.logMessage(ArgumentMatchers.contains("Exception occurred while sending message to event bus"),
					ArgumentMatchers.eq("error"), ArgumentMatchers.any());
	}

	@Test
	void publishEvent_nullDto_failure() {
		String result = this.bankAccountServiceImpl.publishEvent(null);
		then(result).isEqualTo("Failed");
		BDDMockito.then(this.logUtility)
			.should()
			.logMessage(ArgumentMatchers.contains("Exception occurred while sending message to event bus"),
					ArgumentMatchers.eq("error"), ArgumentMatchers.any());
	}

	@Test
	void convertBankAccountDetailsDtoToEventMessage_shouldMapAllFields() throws Exception {
		java.lang.reflect.Method method = BankAccountServiceImpl.class
			.getDeclaredMethod("convertBankAccountDetailsDtoToEventMessage", BankAccountEventDto.class);
		method.setAccessible(true);
		EventBusEvent<BankAccountCreated> event = (EventBusEvent<BankAccountCreated>) method
			.invoke(this.bankAccountServiceImpl, this.bankAccountEventDto);
		then(event.getId()).isEqualTo(this.bankAccountEventDto.getId().toString());
		then(event.getTenantId()).isEqualTo(this.bankAccountEventDto.getTenantId().toString());
		then(event.getEventId()).isEqualTo(this.bankAccountEventDto.getEventId());
		then(event.getPayload()).isEqualTo(this.bankAccountCreated);
		then(event.getType()).isEqualTo("bank_account_created");
		then(event.getCreatedOn()).isNotNull();
	}

	@Test
	void generateKeysIfAbsent_shouldGenerateKeys() throws Exception {
		java.lang.reflect.Field keyMapField = BankAccountServiceImpl.class.getDeclaredField("keyMap");
		keyMapField.setAccessible(true);
		keyMapField.set(this.bankAccountServiceImpl, new HashMap<>());
		AsymmetricCipherKeyPair keyPair = org.mockito.Mockito.mock(AsymmetricCipherKeyPair.class);
		given(this.eccEncryptionService.generateKeyPair()).willReturn(keyPair);
		given(this.eccEncryptionService.getPublicKeyInPemFormat(any())).willReturn("publicKey");
		given(this.eccEncryptionService.getPrivateKeyInPemFormat(any())).willReturn("privateKey");
		java.lang.reflect.Method method = BankAccountServiceImpl.class.getDeclaredMethod("generateKeysIfAbsent");
		method.setAccessible(true);
		method.invoke(this.bankAccountServiceImpl);
		Map<String, String> keyMap = (Map<String, String>) keyMapField.get(this.bankAccountServiceImpl);
		then(keyMap.get("local_public")).isEqualTo("publicKey");
		then(keyMap.get("local_private")).isEqualTo("privateKey");
	}

	@Test
	void generateKeysIfAbsent_shouldNotGenerateIfPresent() throws Exception {
		java.lang.reflect.Field keyMapField = BankAccountServiceImpl.class.getDeclaredField("keyMap");
		keyMapField.setAccessible(true);
		Map<String, String> map = new HashMap<>();
		map.put("local_public", "existingPublic");
		map.put("local_private", "existingPrivate");
		keyMapField.set(this.bankAccountServiceImpl, map);
		java.lang.reflect.Method method = BankAccountServiceImpl.class.getDeclaredMethod("generateKeysIfAbsent");
		method.setAccessible(true);
		method.invoke(this.bankAccountServiceImpl);
		Map<String, String> keyMap = (Map<String, String>) keyMapField.get(this.bankAccountServiceImpl);
		then(keyMap.get("local_public")).isEqualTo("existingPublic");
		then(keyMap.get("local_private")).isEqualTo("existingPrivate");
	}

}
