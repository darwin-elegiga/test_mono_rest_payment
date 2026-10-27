package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.bankaccount.enums.AccountType;
import com.uhg.optum.financials.payments.bankaccount.enums.VerificationStatus;
import com.uhg.optum.financials.payments.bankaccount.events.LocalBankAccountCreated;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.EncryptedLocalBankAccountDto;
import com.uhg.optum.financials.payments.payer.exception.BankAccountException;
import com.uhg.optum.financials.payments.payer.model.events.LocalBankAccount;
import com.uhg.optum.financials.payments.payer.repository.LocalBankAccountRepository;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.core.encryption.EncryptedData;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.util.ReflectionTestUtils;

import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.lenient;

@ExtendWith(MockitoExtension.class)
class LocalBankAccountEventServiceTests {

	@Mock
	private LocalBankAccountRepository localBankAccountRepository;

	@Mock
	private EncryptionAtRestService encryptionAtRestService;

	@Mock
	private LogUtility logUtility;

	private TestableLocalBankAccountEventService localBankAccountEventService;

	@BeforeEach
	void setUp() {
		this.localBankAccountEventService = new TestableLocalBankAccountEventService();
		String encryptedLocalBankAccountEndpoint = "http://localhost/api";
		ReflectionTestUtils.setField(this.localBankAccountEventService, "encryptedLocalBankAccountEndpoint",
				encryptedLocalBankAccountEndpoint);
		ReflectionTestUtils.setField(this.localBankAccountEventService, "logUtility", this.logUtility);
		ReflectionTestUtils.setField(this.localBankAccountEventService, "encryptionAtRestService",
				this.encryptionAtRestService);
		ReflectionTestUtils.setField(this.localBankAccountEventService, "localBankAccountRepository",
				this.localBankAccountRepository);
	}

	@Test
	void shouldNotCreateIfLocalBankAccountAlreadyExists() {
		// given
		UUID id = UUID.randomUUID();
		@SuppressWarnings("unchecked")
		EventBusEvent<LocalBankAccountCreated> event = (EventBusEvent<LocalBankAccountCreated>) mock(
				EventBusEvent.class);
		lenient().when(event.getId()).thenReturn(id.toString());
		lenient().when(this.localBankAccountRepository.findByLocalBankAccountId(id)).thenReturn(new LocalBankAccount());
		// when
		this.localBankAccountEventService.handle(event);
		// then
		// No unnecessary stubbing or verification
	}

	@Test
	void shouldCreateAndEncryptLocalBankAccount() {
		// given
		UUID id = UUID.randomUUID();
		@SuppressWarnings("unchecked")
		EventBusEvent<LocalBankAccountCreated> event = (EventBusEvent<LocalBankAccountCreated>) mock(
				EventBusEvent.class);
		LocalBankAccountCreated payload = mock(LocalBankAccountCreated.class);
		given(event.getId()).willReturn(id.toString());
		given(event.getPayload()).willReturn(payload);
		given(this.localBankAccountRepository.findByLocalBankAccountId(id)).willReturn(null);
		given(payload.getName()).willReturn("Test Name");
		given(payload.getRoutingNumber()).willReturn("123456789");
		given(payload.getAccountNumberLastFourDigits()).willReturn("1234");
		given(payload.getAccountType()).willReturn(AccountType.CHECKING);
		given(payload.getVerificationStatus()).willReturn(VerificationStatus.VERIFIED);
		given(payload.getAchCompanyId()).willReturn("ACH123");
		EncryptedLocalBankAccountDto dto = mock(EncryptedLocalBankAccountDto.class);
		given(dto.getAccountNumber()).willReturn("123456789012");
		this.localBankAccountEventService.setStubDto(dto);
		EncryptedData encryptedData = mock(EncryptedData.class);
		given(encryptedData.getCipherText()).willReturn("encrypted");
		given(encryptedData.getKeyVersion()).willReturn("v1");
		given(this.encryptionAtRestService.encrypt("123456789012")).willReturn(encryptedData);
		// when
		this.localBankAccountEventService.handle(event);
		// then
		verify(this.localBankAccountRepository).save(any(LocalBankAccount.class));
		verify(this.logUtility).logMessage(anyString(), anyString(), any());
	}

	@Test
	void shouldThrowExceptionIfEncryptionFails() {
		// given
		UUID id = UUID.randomUUID();
		@SuppressWarnings("unchecked")
		EventBusEvent<LocalBankAccountCreated> event = (EventBusEvent<LocalBankAccountCreated>) mock(
				EventBusEvent.class);
		LocalBankAccountCreated payload = mock(LocalBankAccountCreated.class);
		given(event.getId()).willReturn(id.toString());
		lenient().when(event.getPayload()).thenReturn(payload);
		lenient().when(this.localBankAccountRepository.findByLocalBankAccountId(id)).thenReturn(null);
		lenient().when(payload.getAccountNumberLastFourDigits()).thenReturn("1234");
		lenient().when(payload.getAccountType()).thenReturn(AccountType.CHECKING);
		lenient().when(payload.getVerificationStatus()).thenReturn(VerificationStatus.VERIFIED);
		lenient().when(payload.getAchCompanyId()).thenReturn("ACH123");
		EncryptedLocalBankAccountDto dto = mock(EncryptedLocalBankAccountDto.class);
		given(dto.getAccountNumber()).willReturn("123456789012");
		this.localBankAccountEventService.setStubDto(dto);
		given(this.encryptionAtRestService.encrypt("123456789012")).willReturn(null);
		// when & then
		assertThatThrownBy(() -> this.localBankAccountEventService.handle(event))
			.isInstanceOf(BankAccountException.class);
		verify(this.logUtility).logMessage(anyString(), anyString(), any());
	}

	private static final class TestableLocalBankAccountEventService extends LocalBankAccountEventService {

		private EncryptedLocalBankAccountDto stubDto;

		@SuppressWarnings("unchecked")
		protected <T extends com.uhg.optum.financials.payments.payer.dto.bankaccount.BaseEncryptedBankAccountDto> T getBankAccountById(
				String endpoint, java.util.UUID id, String token, Class<T> clazz) {
			return (T) this.stubDto;
		}

		void setStubDto(EncryptedLocalBankAccountDto dto) {
			this.stubDto = dto;
		}

	}

}
