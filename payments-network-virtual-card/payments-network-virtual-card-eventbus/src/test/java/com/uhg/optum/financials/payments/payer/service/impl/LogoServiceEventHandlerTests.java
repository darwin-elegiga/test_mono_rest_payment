package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.events.LogoUpdated;
import com.uhg.optum.financials.payments.payer.exception.UpdateEventsException;
import com.uhg.optum.financials.payments.payer.model.Account;
import com.uhg.optum.financials.payments.payer.model.LogoDetails;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.repository.AccountRepository;
import com.uhg.optum.financials.payments.payer.repository.LogoDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Optional;
import java.util.UUID;

import static org.assertj.core.api.BDDAssertions.then;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.contains;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class LogoServiceEventHandlerTests {

	@Mock
	private LogoDetailsRepository logoDetailsRepository;

	@Mock
	private AccountRepository accountRepository;

	@Mock
	private SubAccountRepository subAccountRepository;

	@Mock
	private OrganizationRepository organizationRegisteredRepository;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private LogoServiceEventHandler logoServiceEventHandler;

	@BeforeEach
	void setUp() {
		String baseUrl = "http://localhost/";
		org.springframework.test.util.ReflectionTestUtils.setField(this.logoServiceEventHandler, "baseUrl", baseUrl);
	}

	@Test
	@SuppressWarnings("unchecked")
	void shouldHandleLogoUpdatedEventWithSubAccount() {
		// given
		UUID orgId = UUID.randomUUID();
		String orgShortCode = "ORG123";
		String accountShortCode = "ACC123";
		String subAccountShortCode = "SUB123";
		LogoUpdated logoUpdated = mock(LogoUpdated.class);
		given(logoUpdated.getAccountShortCode()).willReturn(accountShortCode);
		given(logoUpdated.getSubAccountShortCode()).willReturn(subAccountShortCode);
		given(logoUpdated.getType()).willReturn("type");
		given(logoUpdated.getUpdatedBy()).willReturn("user");
		EventBusEvent<LogoUpdated> event = mock(EventBusEvent.class);
		given(event.getPayload()).willReturn(logoUpdated);
		given(event.getId()).willReturn(orgId.toString());
		Organization org = mock(Organization.class);
		given(this.organizationRegisteredRepository.findByOrgId(orgId)).willReturn(org);
		given(org.getOrgShortCode()).willReturn(orgShortCode);
		given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(accountShortCode,
				subAccountShortCode, orgShortCode))
			.willReturn(Optional.of(new SubAccount()));
		given(this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(accountShortCode,
				subAccountShortCode, orgShortCode))
			.willReturn(null);
		// when
		this.logoServiceEventHandler.handle(event);
		// then
		verify(this.logoDetailsRepository).save(any(LogoDetails.class));
		verify(this.logUtility).logMessage(anyString(), anyString(), any());
	}

	@Test
	@SuppressWarnings("unchecked")
	void shouldHandleLogoUpdatedEventWithAccountOnly() {
		// given
		UUID orgId = UUID.randomUUID();
		String orgShortCode = "ORG123";
		String accountShortCode = "ACC123";
		LogoUpdated logoUpdated = mock(LogoUpdated.class);
		given(logoUpdated.getAccountShortCode()).willReturn(accountShortCode);
		given(logoUpdated.getSubAccountShortCode()).willReturn(null);
		given(logoUpdated.getType()).willReturn("type");
		given(logoUpdated.getUpdatedBy()).willReturn("user");
		EventBusEvent<LogoUpdated> event = mock(EventBusEvent.class);
		given(event.getPayload()).willReturn(logoUpdated);
		given(event.getId()).willReturn(orgId.toString());
		Organization org = mock(Organization.class);
		given(this.organizationRegisteredRepository.findByOrgId(orgId)).willReturn(org);
		given(org.getOrgShortCode()).willReturn(orgShortCode);
		Account account = new Account();
		given(this.accountRepository.findByAccountShortCodeAndOrgShortCode(accountShortCode, orgShortCode))
			.willReturn(account);
		given(this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(accountShortCode,
				null, orgShortCode))
			.willReturn(null);
		// when
		this.logoServiceEventHandler.handle(event);
		// then
		verify(this.logoDetailsRepository).save(any(LogoDetails.class));
		verify(this.logUtility).logMessage(anyString(), anyString(), any());
	}

	@Test
	@SuppressWarnings("unchecked")
	void shouldHandleLogoUpdatedEventWithOrganizationOnly() {
		// given
		UUID orgId = UUID.randomUUID();
		String orgShortCode = "ORG123";
		LogoUpdated logoUpdated = mock(LogoUpdated.class);
		given(logoUpdated.getAccountShortCode()).willReturn(null);
		given(logoUpdated.getSubAccountShortCode()).willReturn(null);
		given(logoUpdated.getType()).willReturn("type");
		given(logoUpdated.getUpdatedBy()).willReturn("user");
		EventBusEvent<LogoUpdated> event = mock(EventBusEvent.class);
		given(event.getPayload()).willReturn(logoUpdated);
		given(event.getId()).willReturn(orgId.toString());
		Organization org = mock(Organization.class);
		given(this.organizationRegisteredRepository.findByOrgId(orgId)).willReturn(org);
		given(org.getOrgShortCode()).willReturn(orgShortCode);
		given(this.organizationRegisteredRepository.findByOrgShortCode(orgShortCode)).willReturn(org);
		given(this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(null, null,
				orgShortCode))
			.willReturn(null);
		// when
		this.logoServiceEventHandler.handle(event);
		// then
		verify(this.logoDetailsRepository).save(any(LogoDetails.class));
		verify(this.logUtility).logMessage(anyString(), anyString(), any());
	}

	@Test
	@SuppressWarnings("unchecked")
	void shouldThrowUpdateEventsExceptionOnError() {
		// given
		UUID orgId = UUID.randomUUID();
		String orgShortCode = "ORG123";
		LogoUpdated logoUpdated = mock(LogoUpdated.class);
		given(logoUpdated.getAccountShortCode()).willReturn(null);
		given(logoUpdated.getSubAccountShortCode()).willReturn(null);
		given(logoUpdated.getType()).willReturn("type");
		given(logoUpdated.getUpdatedBy()).willReturn("user");
		EventBusEvent<LogoUpdated> event = mock(EventBusEvent.class);
		given(event.getPayload()).willReturn(logoUpdated);
		given(event.getId()).willReturn(orgId.toString());
		Organization org = mock(Organization.class);
		given(this.organizationRegisteredRepository.findByOrgId(orgId)).willReturn(org);
		given(org.getOrgShortCode()).willReturn(orgShortCode);
		given(this.organizationRegisteredRepository.findByOrgShortCode(orgShortCode))
			.willThrow(new RuntimeException("DB error"));
		given(this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(null, null,
				orgShortCode))
			.willReturn(null);
		// when & then
		assertThatThrownBy(() -> this.logoServiceEventHandler.handle(event)).isInstanceOf(UpdateEventsException.class);
		verify(this.logUtility).logMessageWithPriority(contains("DB error"), anyString(), any(), anyString());
	}

	@Test
	void shouldReturnOrgShortCodeFromGetOrgShortCode() throws Exception {
		// given
		UUID orgId = UUID.randomUUID();
		Organization org = mock(Organization.class);
		given(this.organizationRegisteredRepository.findByOrgId(orgId)).willReturn(org);
		given(org.getOrgShortCode()).willReturn("ORG123");
		// when
		java.lang.reflect.Method method = this.logoServiceEventHandler.getClass()
			.getDeclaredMethod("getOrgShortCode", UUID.class);
		method.setAccessible(true);
		String result = (String) method.invoke(this.logoServiceEventHandler, orgId);
		// then
		then(result).isEqualTo("ORG123");
	}

	@Test
	void shouldReturnNewLogoDetailsIfNotFound() throws Exception {
		// given
		String orgShortCode = "ORG123";
		LogoUpdated logoUpdated = mock(LogoUpdated.class);
		given(logoUpdated.getAccountShortCode()).willReturn("ACC123");
		given(logoUpdated.getSubAccountShortCode()).willReturn("SUB123");
		given(logoUpdated.getType()).willReturn("type");
		given(logoUpdated.getUpdatedBy()).willReturn("user");
		given(this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACC123", "SUB123",
				orgShortCode))
			.willReturn(null);
		// when
		java.lang.reflect.Method method = this.logoServiceEventHandler.getClass()
			.getDeclaredMethod("getLogoDetails", String.class, LogoUpdated.class);
		method.setAccessible(true);
		LogoDetails result = (LogoDetails) method.invoke(this.logoServiceEventHandler, orgShortCode, logoUpdated);
		// then
		then(result.getOrgShortCode()).isEqualTo(orgShortCode);
		then(result.getType()).isEqualTo("type");
		then(result.getCreatedById()).isEqualTo("virtual-card");
		then(result.getUpdatedById()).isEqualTo("user");
		then(result.getCreatedOn()).isNotNull();
	}

	@Test
	void shouldUpdateLogoDetailsIfFound() throws Exception {
		// given
		String orgShortCode = "ORG123";
		LogoUpdated logoUpdated = mock(LogoUpdated.class);
		given(logoUpdated.getAccountShortCode()).willReturn("ACC123");
		given(logoUpdated.getSubAccountShortCode()).willReturn("SUB123");
		given(this.logoDetailsRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACC123", "SUB123",
				orgShortCode))
			.willReturn(new LogoDetails());
		// when
		java.lang.reflect.Method method = this.logoServiceEventHandler.getClass()
			.getDeclaredMethod("getLogoDetails", String.class, LogoUpdated.class);
		method.setAccessible(true);
		LogoDetails result = (LogoDetails) method.invoke(this.logoServiceEventHandler, orgShortCode, logoUpdated);
		// then
		then(result.getLastUpdate()).isNotNull();
	}

}
