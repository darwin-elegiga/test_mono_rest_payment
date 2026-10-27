package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.bankaccount.events.BankAccountChanged;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountClosed;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountUnverified;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountVerified;
import com.uhg.optum.financials.payments.payer.repository.BankAccountRepository;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.service.BaseBankAccountService;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.payments.payer.model.events.BankAccount;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.springframework.context.ApplicationEventPublisher;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.BDDMockito.willDoNothing;
import static org.mockito.Mockito.mock;

class BankAccountEventHandlerTests {

	@Mock
	private BaseBankAccountService baseBankAccountService;

	@Mock
	private ApplicationEventPublisher eventPublisher;

	@Mock
	private BankAccountRepository bankAccountRepository;

	@Mock
	private OrganizationRepository organizationRepository;

	@Mock
	private EncryptionAtRestService encryptionAtRestService;

	@Mock
	private SecretService secretService;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private BankAccountEventHandler handler;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
		// Mock secretService for getBankAccountById
		var mockKeys = mock(com.uhg.optum.financials.shared.core.secrets.AssymetricSecretKeys.class);
		given(mockKeys.getPublicKeyValue()).willReturn("public-key");
		given(mockKeys.getPrivateKeyValue()).willReturn("private-key");
		given(this.secretService.getAssymetricEncryptionKeys(any(), any())).willReturn(mockKeys);
		// Mock logUtility to avoid NPEs
		willDoNothing().given(this.logUtility).logMessage(anyString(), anyString(), any());
		willDoNothing().given(this.logUtility).logMessageWithPriority(anyString(), anyString(), any(), anyString());
	}

	@Test
	@DisplayName("should handle BankAccountChanged event and update account")
	void givenBankAccountChanged_whenHandle_thenUpdateCalled() {
		BankAccountChanged payload = mock(BankAccountChanged.class);
		EventBusEvent<BankAccountChanged> event = mock(EventBusEvent.class);
		given(event.getPayload()).willReturn(payload);
		given(event.getId()).willReturn("00000000-0000-0000-0000-000000000002");
		BankAccount account = new BankAccount();
		given(this.bankAccountRepository.findByBankAccountId(any())).willReturn(account);
		this.handler.handle(event);
		then(this.bankAccountRepository).should().save(account);
	}

	@Test
	@DisplayName("should handle BankAccountVerified event and update account")
	void givenBankAccountVerified_whenHandle_thenUpdateCalled() {
		BankAccountVerified payload = mock(BankAccountVerified.class);
		EventBusEvent<BankAccountVerified> event = mock(EventBusEvent.class);
		given(event.getPayload()).willReturn(payload);
		given(event.getId()).willReturn("00000000-0000-0000-0000-000000000003");
		BankAccount account = new BankAccount();
		account.setVerificationStatus("NotVerified");
		given(this.bankAccountRepository.findByBankAccountId(any())).willReturn(account);
		this.handler.handle(event);
		then(this.bankAccountRepository).should().save(account);
	}

	@Test
	@DisplayName("should handle BankAccountUnverified event and update account")
	void givenBankAccountUnverified_whenHandle_thenUpdateCalled() {
		BankAccountUnverified payload = mock(BankAccountUnverified.class);
		EventBusEvent<BankAccountUnverified> event = mock(EventBusEvent.class);
		given(event.getPayload()).willReturn(payload);
		given(event.getId()).willReturn("00000000-0000-0000-0000-000000000004");
		BankAccount account = new BankAccount();
		account.setVerificationStatus("Verified");
		given(this.bankAccountRepository.findByBankAccountId(any())).willReturn(account);
		this.handler.handle(event);
		then(this.bankAccountRepository).should().save(account);
	}

	@Test
	@DisplayName("should handle BankAccountClosed event and update account")
	void givenBankAccountClosed_whenHandle_thenUpdateCalled() {
		BankAccountClosed payload = mock(BankAccountClosed.class);
		EventBusEvent<BankAccountClosed> event = mock(EventBusEvent.class);
		given(event.getPayload()).willReturn(payload);
		given(event.getId()).willReturn("00000000-0000-0000-0000-000000000005");
		BankAccount account = new BankAccount();
		account.setVerificationStatus("Verified");
		given(this.bankAccountRepository.findByBankAccountId(any())).willReturn(account);
		this.handler.handle(event);
		then(this.bankAccountRepository).should().save(account);
	}

}
