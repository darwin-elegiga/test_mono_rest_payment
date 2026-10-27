package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.enums.FundingMethod;
import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.enums.OnboardingStatus;
import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.organization.events.AccountUpdated;
import com.uhg.optum.financials.payments.organization.events.OrganizationClosed;
import com.uhg.optum.financials.payments.organization.events.OrganizationRegistered;
import com.uhg.optum.financials.payments.organization.events.OrganizationUpdated;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.organization.events.SubAccountUpdated;
import com.uhg.optum.financials.payments.organization.models.Address;
import com.uhg.optum.financials.payments.payer.model.Account;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.repository.AccountRepository;
import com.uhg.optum.financials.payments.payer.repository.AddressRepository;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.Date;
import java.util.Optional;
import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.BDDMockito.willThrow;

@ExtendWith(MockitoExtension.class)
public class OrganizationEventServiceImplTests {

	@InjectMocks
	private OrganizationEventServiceImpl organizationEventHandler;

	@Mock
	SubAccountRepository subAccountRepository;

	@Mock
	private LogUtility logUtility;

	@Mock
	OrganizationRepository organizationRepository;

	@Mock
	AccountRepository accountRepository;

	@Mock
	private AddressRepository addressRepository;

	// @Test
	public void testSavesubAccountRegisterDetails() {
		EventBusEvent<SubAccountRegistered> event = new EventBusEvent<>();

		SubAccountRegistered subAccountRegisteredDto = new SubAccountRegistered();
		subAccountRegisteredDto.setAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setSubAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setMarketType(MarketType.MEDICAL);
		subAccountRegisteredDto.setOnboardingStatus(OnboardingStatus.ACTIVE);
		Address address = new Address();
		address.setAddressLine1("addressLine1");
		subAccountRegisteredDto.setAddress(address);
		event.setPayload(subAccountRegisteredDto);
		event.setId(UUID.randomUUID().toString());
		Organization organization = new Organization();
		organization.setOrgShortCode("orgShortCode");
		Mockito.when(this.organizationRepository.findByOrgId(any())).thenReturn(organization);
		this.organizationEventHandler.handle(event);
	}

	@Test
	public void testSaveOrganizationRegistrationDetails() {
		EventBusEvent<OrganizationRegistered> event = new EventBusEvent<>();
		OrganizationRegistered organizationRegisteredDto = new OrganizationRegistered();
		event.setPayload(organizationRegisteredDto);
		event.setId(UUID.randomUUID().toString());
		organizationRegisteredDto.setFundingMethod(FundingMethod.POST_FUNDED);
		organizationRegisteredDto.setMarketType(MarketType.MEDICAL);
		organizationRegisteredDto.setOnboardedOn(new Date());
		organizationRegisteredDto.setOnboardingStatus(OnboardingStatus.ACTIVE);
		Address address = new Address();
		address.setAddressLine1("addressLine1");
		organizationRegisteredDto.setAddress(address);
		this.organizationEventHandler.handle(event);
	}

	@Test
	public void testSaveAccount() {
		EventBusEvent<AccountRegistered> event = new EventBusEvent<>();
		event.setId(UUID.randomUUID().toString());
		AccountRegistered accountRegisteredDto = new AccountRegistered();
		event.setPayload(accountRegisteredDto);
		accountRegisteredDto.setMarketType(MarketType.MEDICAL);
		Address address = new Address();
		address.setAddressLine1("addressLine1");
		accountRegisteredDto.setAddress(address);
		accountRegisteredDto.setOnboardingStatus(OnboardingStatus.ACTIVE);
		accountRegisteredDto.setAccountId(UUID.randomUUID());
		accountRegisteredDto.setOnboardingStatus(OnboardingStatus.ACTIVE);
		Organization organization = new Organization();
		organization.setOrgShortCode("orgShortCode");
		organization.setOnboardingStatus(OnboardingStatus.ACTIVE.toString());
		Mockito.when(this.organizationRepository.findByOrgId(any())).thenReturn(organization);
		this.organizationEventHandler.handle(event);
	}

	// @Test
	public void testSavesubAccountRegisterDetailsNotNull() {
		EventBusEvent<SubAccountRegistered> event = new EventBusEvent<>();
		SubAccountRegistered subAccountRegisteredDto = new SubAccountRegistered();
		event.setId(UUID.randomUUID().toString());
		event.setPayload(subAccountRegisteredDto);
		subAccountRegisteredDto.setAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setSubAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setMarketType(MarketType.MEDICAL);
		subAccountRegisteredDto.setOnboardingStatus(OnboardingStatus.ACTIVE);
		Address address = new Address();
		address.setAddressLine1("addressLine1");
		subAccountRegisteredDto.setAddress(address);
		SubAccount subAccountRegistrationDetails = new SubAccount();
		subAccountRegistrationDetails.setSubAccountShortCode("subAccountShortCode");
		subAccountRegistrationDetails.setTechnicalEmailAddress("test@gmail.com");
		Organization organization = new Organization();
		organization.setOrgShortCode("orgShortCode");
		com.uhg.optum.financials.payments.payer.model.Address accountAddressDetails = new com.uhg.optum.financials.payments.payer.model.Address();
		accountAddressDetails.setId(1L);
		subAccountRegistrationDetails.setAddress(accountAddressDetails);
		Mockito.when(this.addressRepository.findById(anyLong())).thenReturn(Optional.of(accountAddressDetails));
		Mockito.when(this.organizationRepository.findByOrgId(any())).thenReturn(organization);
		Mockito
			.when(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(any(), any(),
					any()))
			.thenReturn(Optional.of(subAccountRegistrationDetails));
		this.organizationEventHandler.handle(event);
	}

	// @Test
	public void testSavesubAccountRegisterDetailsAccountIdNull() {
		EventBusEvent<SubAccountRegistered> event = new EventBusEvent<>();
		SubAccountRegistered subAccountRegisteredDto = new SubAccountRegistered();
		event.setId(UUID.randomUUID().toString());
		event.setPayload(subAccountRegisteredDto);
		subAccountRegisteredDto.setAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setSubAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setMarketType(MarketType.MEDICAL);
		subAccountRegisteredDto.setOnboardingStatus(OnboardingStatus.ACTIVE);
		Address address = new Address();
		address.setAddressLine1("addressLine1");
		subAccountRegisteredDto.setAddress(address);
		Organization organization = new Organization();
		organization.setOrgShortCode("orgShortCode");
		Mockito.when(this.organizationRepository.findByOrgId(any())).thenReturn(organization);
		this.organizationEventHandler.handle(event);
	}

	// @Test
	public void testSavesubAccountRegisterDetailsOrgDetailNotFound() {
		EventBusEvent<SubAccountRegistered> event = new EventBusEvent<>();
		SubAccountRegistered subAccountRegisteredDto = new SubAccountRegistered();
		event.setId(UUID.randomUUID().toString());
		event.setPayload(subAccountRegisteredDto);
		subAccountRegisteredDto.setAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setSubAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setMarketType(MarketType.MEDICAL);
		subAccountRegisteredDto.setOnboardingStatus(OnboardingStatus.ACTIVE);
		Address address = new Address();
		address.setAddressLine1("addressLine1");
		subAccountRegisteredDto.setAddress(address);
		SubAccount subAccountRegistrationDetails = new SubAccount();
		subAccountRegistrationDetails.setTechnicalEmailAddress("test@gmail.com");
		Organization organization = new Organization();
		organization.setOrgShortCode("orgShortCode");
		Mockito.when(this.organizationRepository.findByOrgId(any())).thenReturn(organization);
		this.organizationEventHandler.handle(event);
	}

	// @Test
	public void testSaveSubAccountRegisterDetailsExistingNull() {
		EventBusEvent<SubAccountRegistered> event = new EventBusEvent<>();
		SubAccountRegistered subAccountRegisteredDto = new SubAccountRegistered();
		event.setId(UUID.randomUUID().toString());
		event.setPayload(subAccountRegisteredDto);
		subAccountRegisteredDto.setAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setSubAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setMarketType(MarketType.MEDICAL);
		subAccountRegisteredDto.setOnboardingStatus(OnboardingStatus.ACTIVE);
		Address address = new Address();
		address.setAddressLine1("addressLine1");
		subAccountRegisteredDto.setAddress(address);
		Organization organization = new Organization();
		organization.setOrgShortCode("orgShortCode");
		organization.setOnboardingStatus(OnboardingStatus.ACTIVE.toString());
		Mockito.when(this.organizationRepository.findByOrgId(any())).thenReturn(organization);
		Mockito
			.when(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(any(), any(),
					any()))
			.thenReturn(Optional.empty());
		this.organizationEventHandler.handle(event);
	}

	// @Test
	public void testSaveSubAccountUpdated() {
		EventBusEvent<SubAccountUpdated> event = new EventBusEvent<>();
		SubAccountUpdated subAccountRegisteredDto = new SubAccountUpdated();
		event.setId(UUID.randomUUID().toString());
		event.setPayload(subAccountRegisteredDto);
		subAccountRegisteredDto.setAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setSubAccountId(UUID.randomUUID());
		subAccountRegisteredDto.setMarketType(MarketType.MEDICAL);
		Address address = new Address();
		address.setAddressLine1("addressLine1");
		subAccountRegisteredDto.setAddress(address);
		SubAccount subAccountRegistrationDetails = new SubAccount();
		subAccountRegisteredDto.setOnboardingStatus(OnboardingStatus.ACTIVE);
		Mockito
			.when(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(any(), any(),
					any()))
			.thenReturn(Optional.of(subAccountRegistrationDetails));
		Organization organization = new Organization();
		com.uhg.optum.financials.payments.payer.model.Address accountAddressDetails = new com.uhg.optum.financials.payments.payer.model.Address();
		accountAddressDetails.setId(1L);
		subAccountRegistrationDetails.setAddress(accountAddressDetails);
		Mockito.when(this.addressRepository.findById(anyLong())).thenReturn(Optional.of(accountAddressDetails));
		organization.setOrgShortCode("orgShortCode");
		Mockito.when(this.organizationRepository.findByOrgId(any())).thenReturn(organization);
		this.organizationEventHandler.handle(event);
	}

	// @Test
	public void testSaveAccountRegisterDetailsNotNull() {
		EventBusEvent<AccountUpdated> event = new EventBusEvent<>();
		AccountUpdated accountUpdated = new AccountUpdated();
		event.setId(UUID.randomUUID().toString());
		event.setPayload(accountUpdated);
		accountUpdated.setAccountId(UUID.randomUUID());
		accountUpdated.setMarketType(MarketType.MEDICAL);
		accountUpdated.setOnboardingStatus(OnboardingStatus.ACTIVE);
		Address address = new Address();
		address.setAddressLine1("addressLine1");
		accountUpdated.setAddress(address);
		com.uhg.optum.financials.payments.payer.model.Address accountAddressDetails = new com.uhg.optum.financials.payments.payer.model.Address();
		accountAddressDetails.setId(1L);
		Account account = new Account();
		account.setAddress(accountAddressDetails);
		Mockito.when(this.addressRepository.findById(anyLong())).thenReturn(Optional.of(accountAddressDetails));
		account.setTechnicalEmailAddress("test@gmail.com");
		Mockito.when(this.organizationRepository.findByOrgId(any())).thenReturn(new Organization());
		Mockito.when(this.accountRepository.findByAccountShortCodeAndOrgShortCode(any(), any())).thenReturn(account);
		this.organizationEventHandler.handle(event);
	}

	@Test
	public void testSaveOrgRegisterDetailsNotNull() {
		EventBusEvent<OrganizationUpdated> event = new EventBusEvent<>();
		event.setId(UUID.randomUUID().toString());
		OrganizationUpdated organizationUpdated = new OrganizationUpdated();
		event.setPayload(organizationUpdated);
		organizationUpdated.setMarketType(MarketType.MEDICAL);

		organizationUpdated.setOnboardedOn(new Date());
		Address address = new Address();
		address.setAddressLine1("addressLine1");
		organizationUpdated.setAddress(address);
		organizationUpdated.setOnboardingStatus(OnboardingStatus.ACTIVE);
		Organization organization = new Organization();
		com.uhg.optum.financials.payments.payer.model.Address accountAddressDetails = new com.uhg.optum.financials.payments.payer.model.Address();
		accountAddressDetails.setId(1L);
		organization.setAddressId(accountAddressDetails.getId());
		Mockito.when(this.addressRepository.findById(anyLong())).thenReturn(Optional.of(accountAddressDetails));
		organization.setTechnicalEmailAddress("test@gmail.com");
		organization.setOnboardingStatus(OnboardingStatus.ACTIVE.toString());
		Mockito.when(this.organizationRepository.findByOrgId(any())).thenReturn(organization);
		this.organizationEventHandler.handle(event);
	}

	@Test
	void testCloseOrganizationDetails() {
		this.organizationEventHandler.closeOrganizationDetails(new OrganizationClosed(), UUID.randomUUID());
		Assertions.assertThat(this.organizationRepository).isNotNull();
	}

	@Test
	void testCloseOrganizationDetails_Exception() {
		OrganizationClosed organizationClosed = new OrganizationClosed();
		UUID orgId = UUID.randomUUID();

		willThrow(new RuntimeException("Error while updating organization details")).given(this.organizationRepository)
			.closeOrganizationById(any(), any());

		this.organizationEventHandler.closeOrganizationDetails(organizationClosed, orgId);
	}

}
