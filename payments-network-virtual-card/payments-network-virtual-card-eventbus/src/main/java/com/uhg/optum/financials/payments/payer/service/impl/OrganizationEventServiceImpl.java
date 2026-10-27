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
import com.uhg.optum.financials.payments.organization.models.Contact;
import com.uhg.optum.financials.payments.organization.models.FundingAccounts;
import com.uhg.optum.financials.payments.organization.models.NetworkExpiration;
import com.uhg.optum.financials.payments.organization.models.SecondaryContact;
import com.uhg.optum.financials.payments.payer.constant.ValidationConstants;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.EncryptedBankAccountDto;
import com.uhg.optum.financials.payments.payer.enums.OrganizationStatus;
import com.uhg.optum.financials.payments.payer.exception.BankAccountException;
import com.uhg.optum.financials.payments.payer.exception.RegisterEventsException;
import com.uhg.optum.financials.payments.payer.exception.UpdateEventsException;
import com.uhg.optum.financials.payments.payer.mapper.SubAccountMapper;
import com.uhg.optum.financials.payments.payer.model.Account;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.model.events.BankAccount;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.repository.AccountRepository;
import com.uhg.optum.financials.payments.payer.repository.AddressRepository;
import com.uhg.optum.financials.payments.payer.repository.BankAccountRepository;
import com.uhg.optum.financials.payments.payer.repository.LocalBankAccountRepository;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.BaseBankAccountService;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.service.IEventBusHandler;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.client.events.IEvent;
import com.uhg.optum.financials.shared.core.encryption.EncryptedData;
import jakarta.transaction.Transactional;
import lombok.Builder;
import lombok.Getter;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.Arrays;
import java.util.Date;
import java.util.UUID;

import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.ORGANIZATION_REGISTERED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.ORGANIZATION_UPDATED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ACC_EXISTS_ERROR_LOGS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ACC_NOT_FOUND_ERROR_LOGS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ADD_ACCOUNT_ERROR_LOGS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ADD_SUB_ACCOUNT_ERROR_LOGS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.DEFAULT_HAS_CUSTOM_LOGO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ORG_NOT_FOUND_ERROR_LOGS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ORG_NOT_FOUND_ID_ERROR_LOGS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.UPDATE_ACCOUNT_ERROR_LOGS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.UPDATE_SUB_ACCOUNT_ERROR_LOGS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD_NETWORK_ID;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;
import static com.uhg.optum.financials.payments.payer.mapper.AccountMapper.toEntity;

@Slf4j
@Service("organizationEventHandler")
public class OrganizationEventServiceImpl extends BaseBankAccountService implements IEventBusHandler {

	@Value("${base.url}")
	private String baseUrl;

	@Autowired
	private SubAccountRepository subAccountRepository;

	@Autowired
	private LogUtility logUtility;

	@Autowired
	private OrganizationRepository organizationRepository;

	@Autowired
	private AccountRepository accountRepository;

	@Autowired
	private AddressRepository addressRepository;

	@Autowired
	BankAccountRepository bankAccountRepository;

	@Autowired
	LocalBankAccountRepository localBankAccountRepository;

	@Value("${endpoint.encrypted.bank-account}")
	private String encryptedBankAccountEndpoint;

	@Autowired
	private EncryptionAtRestService encryptionAtRestService;

	@Override
	@SuppressWarnings({ "unchecked" })
	public <T extends IEvent> void handle(EventBusEvent<T> message) {
		log.debug("Received event: {}", message.getPayload().getClass().getSimpleName());
		if (message.getPayload() instanceof OrganizationRegistered) {
			handleOrganizationRegistered((EventBusEvent<OrganizationRegistered>) message);
		}
		else if (message.getPayload() instanceof OrganizationUpdated) {
			handleOrganizationUpdated((EventBusEvent<OrganizationUpdated>) message);
		}
		else if (message.getPayload() instanceof OrganizationClosed) {
			handleOrganizationClosed((EventBusEvent<OrganizationClosed>) message);
		}
		else if (message.getPayload() instanceof AccountRegistered) {
			handleAccountRegistered((EventBusEvent<AccountRegistered>) message);
		}
		else if (message.getPayload() instanceof AccountUpdated) {
			handleAccountUpdated((EventBusEvent<AccountUpdated>) message);
		}
		else if (message.getPayload() instanceof SubAccountRegistered) {
			handleSubAccountRegistered((EventBusEvent<SubAccountRegistered>) message);
		}
		else if (message.getPayload() instanceof SubAccountUpdated) {
			handleSubAccountUpdated((EventBusEvent<SubAccountUpdated>) message);
		}
	}

	private void handleOrganizationRegistered(EventBusEvent<OrganizationRegistered> event) {
		log.debug("Organization registered event received");
		OrganizationRegistered organizationRegistered = event.getPayload();
		var contact = (organizationRegistered.getContact() != null) ? organizationRegistered.getContact()
				: new Contact();
		var claim = (organizationRegistered.getClaimOfficeContact() != null)
				? organizationRegistered.getClaimOfficeContact() : new SecondaryContact();
		var technical = (organizationRegistered.getTechnicalContact() != null)
				? organizationRegistered.getTechnicalContact() : new SecondaryContact();
		OrganizationRecord organizationRecord = OrganizationRecord.builder()
			.id(UUID.fromString(event.getId()))
			.marketType(organizationRegistered.getMarketType())
			.orgStatus(OrganizationStatus.ACTIVE.toString())
			.name(organizationRegistered.getName())
			.address(organizationRegistered.getAddress())
			.contactName(contact.getContactName())
			.faxNumber(contact.getFaxNumber())
			.phoneNumber(contact.getPhoneNumber())
			.contactEmail(contact.getEmailAddress())
			.tin(organizationRegistered.getTin())
			.onboardedOn(organizationRegistered.getOnboardedOn())
			.programId(organizationRegistered.getProgramId())
			.fundingMethod(organizationRegistered.getFundingMethod())
			.claimOfficePhoneNumber(claim.getPhoneNumber())
			.claimOfficePhoneNumberExtension(claim.getPhoneNumberExtension())
			.claimOfficeUrl(claim.getUrl())
			.technicalContactName(technical.getContactName())
			.technicalPhoneNumber(technical.getPhoneNumber())
			.technicalPhoneNumberExtension(technical.getPhoneNumberExtension())
			.technicalUrl(technical.getUrl())
			.technicalFaxNumber(technical.getFaxNumber())
			.technicalEmailAddress(technical.getEmailAddress())
			.onboardingStatus((organizationRegistered.getOnboardingStatus() == null) ? OnboardingStatus.PENDING
					: organizationRegistered.getOnboardingStatus())
			.shortCode(organizationRegistered.getShortCode())
			.build();
		saveOrganization(organizationRecord, event.getId());
	}

	private void handleOrganizationUpdated(EventBusEvent<OrganizationUpdated> event) {
		log.debug("Organization updated event received");
		OrganizationUpdated organizationUpdated = event.getPayload();
		var contact = (organizationUpdated.getContact() != null) ? organizationUpdated.getContact() : new Contact();
		var claim = (organizationUpdated.getClaimOfficeContact() != null) ? organizationUpdated.getClaimOfficeContact()
				: new SecondaryContact();
		var technical = (organizationUpdated.getTechnicalContact() != null) ? organizationUpdated.getTechnicalContact()
				: new SecondaryContact();
		OrganizationRecord organizationRecord = OrganizationRecord.builder()
			.id(UUID.fromString(event.getId()))
			.marketType(organizationUpdated.getMarketType())
			.orgStatus(OrganizationStatus.ACTIVE.toString())
			.name(organizationUpdated.getName())
			.address(organizationUpdated.getAddress())
			.contactName(contact.getContactName())
			.faxNumber(contact.getFaxNumber())
			.phoneNumber(contact.getPhoneNumber())
			.contactEmail(contact.getEmailAddress())
			.tin(organizationUpdated.getTin())
			.onboardedOn(organizationUpdated.getOnboardedOn())
			.programId(organizationUpdated.getProgramId())
			.fundingMethod(organizationUpdated.getFundingMethod())
			.claimOfficePhoneNumber(claim.getPhoneNumber())
			.claimOfficePhoneNumberExtension(claim.getPhoneNumberExtension())
			.claimOfficeUrl(claim.getUrl())
			.technicalContactName(technical.getContactName())
			.technicalPhoneNumber(technical.getPhoneNumber())
			.technicalPhoneNumberExtension(technical.getPhoneNumberExtension())
			.technicalUrl(technical.getUrl())
			.technicalFaxNumber(technical.getFaxNumber())
			.technicalEmailAddress(technical.getEmailAddress())
			.shortCode(event.getTenantId())
			.onboardingStatus((organizationUpdated.getOnboardingStatus() == null) ? OnboardingStatus.PENDING
					: organizationUpdated.getOnboardingStatus())
			.build();
		saveOrganization(organizationRecord, event.getId());
	}

	private void handleOrganizationClosed(EventBusEvent<OrganizationClosed> event) {
		log.debug("Organization Close event received");
		OrganizationClosed organizationClosed = event.getPayload();
		closeOrganizationDetails(organizationClosed, UUID.fromString(event.getId()));
	}

	private void handleAccountRegistered(EventBusEvent<AccountRegistered> event) {
		AccountRegistered accountRegistered = event.getPayload();
		try {
			// Checking for ORG
			if (this.checkIfOrgExists(UUID.fromString(event.getId()))) {
				// Creating Account
				this.saveAccount(accountRegistered, event.getId());
			}
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority(ADD_ACCOUNT_ERROR_LOGS + ex.getMessage(), ValidationConstants.ERROR,
					log, P3);
			throw new RegisterEventsException(ex.getMessage());
		}
	}

	private void handleAccountUpdated(EventBusEvent<AccountUpdated> event) {
		AccountUpdated accountUpdated = event.getPayload();
		try {
			UUID orgID = UUID.fromString(event.getId());
			// Checking for ORG
			if (this.checkIfOrgExists(orgID)) {
				String orgShortCode = this.getOrgShortCode(orgID);

				Account payer = this.accountRepository
					.findByAccountShortCodeAndOrgShortCode(accountUpdated.getShortCode(), orgShortCode);
				if (payer != null) {
					payer.setUpdatedById(accountUpdated.getUpdatedBy());
					payer.setUpdatedOn(LocalDateTime.now());
				}
				else {
					// Creating new Account if not exists
					payer = new Account();
					payer.setCreatedOn(LocalDateTime.now());
					payer.setCreatedById(accountUpdated.getUpdatedBy());
					payer.setOrgShortCode(orgShortCode);
				}

				toEntity(accountUpdated, payer);
				this.accountRepository.save(payer);

				this.logUtility
					.logMessage("account details Created/Updated successfully for the given accountShortCode : "
							+ accountUpdated.getShortCode(), INFO, log);
			}
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority(UPDATE_ACCOUNT_ERROR_LOGS + ex.getMessage(),
					ValidationConstants.ERROR, log, P3);
			throw new UpdateEventsException(ex.getMessage());
		}
	}

	private void handleSubAccountRegistered(EventBusEvent<SubAccountRegistered> event) {
		SubAccountRegistered subAccountRegistered = event.getPayload();

		UUID orgId = UUID.fromString(event.getId());

		try {
			if (this.checkOrgExistElseRaiseP3(orgId)) {
				String orgShortCode = getOrgShortCode(orgId);

				if (this.isAccountExist(subAccountRegistered.getAccountShortCode(), orgShortCode)) {
					saveSubAccount(subAccountRegistered, orgShortCode);
				}
				else {
					this.logUtility.logMessageWithPriority(ACC_NOT_FOUND_ERROR_LOGS
							+ subAccountRegistered.getAccountShortCode() + " for org shortcode: " + orgShortCode,
							ValidationConstants.ERROR, log, P3);
				}
			}
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority(ADD_SUB_ACCOUNT_ERROR_LOGS + ex.getMessage(),
					ValidationConstants.ERROR, log, P3);
			throw new RegisterEventsException(ex.getMessage());
		}

	}

	private void handleSubAccountUpdated(EventBusEvent<SubAccountUpdated> event) {
		UUID orgId = UUID.fromString(event.getId());
		SubAccountUpdated subAccountUpdated = event.getPayload();
		try {
			if (this.checkOrgExistElseRaiseP3(orgId)) {
				String orgShortCode = getOrgShortCode(orgId);
				if (this.isAccountExist(subAccountUpdated.getAccountShortCode(), orgShortCode)) {
					this.updateSubPayerDetails(subAccountUpdated, orgShortCode);
				}
				else {
					this.logUtility.logMessageWithPriority(
							ACC_NOT_FOUND_ERROR_LOGS + subAccountUpdated.getAccountShortCode() + " for this org : "
									+ orgShortCode + " updateSubPayer ",
							ValidationConstants.ERROR, log, P3);

				}
			}
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority(UPDATE_SUB_ACCOUNT_ERROR_LOGS + ex.getMessage(),
					ValidationConstants.ERROR, log, P3);
			throw new UpdateEventsException(ex.getMessage());
		}
	}

	@Transactional(rollbackOn = Exception.class)
	private void saveSubAccount(SubAccountRegistered subAccountRegistered, String orgShortCode) {
		Integer daysToExpire = getExpiryDaysForVirtualCard(subAccountRegistered.getNetworkExpirations());
		if (this.isSubAccountExists(subAccountRegistered.getAccountShortCode(), subAccountRegistered.getShortCode(),
				orgShortCode)) {
			this.logUtility.logMessage("subAccount already exists with the given subAccountShortCode: "
					+ subAccountRegistered.getShortCode(), ValidationConstants.ERROR, log);
		}
		else {
			SubAccount subAccount = SubAccountMapper.toEntity(subAccountRegistered);
			subAccount.setCreatedOn(LocalDateTime.now());
			subAccount.setCreatedById(subAccountRegistered.getCreatedBy());
			subAccount.setOrgShortCode(orgShortCode);
			if (subAccountRegistered.getFundingAccounts() != null) {
				this.updateFundingAccounts(subAccountRegistered.getFundingAccounts(), orgShortCode, subAccount);
			}
			subAccount.setExpiryDays(daysToExpire);
			this.subAccountRepository.save(subAccount);

			this.logUtility.logMessage("subAccounts details saved successfully with this short code : "
					+ subAccountRegistered.getShortCode(), INFO, log);
		}
	}

	private void updateSubPayerDetails(SubAccountUpdated subAccountUpdated, String orgShortCode) {

		Integer daysToExpire = getExpiryDaysForVirtualCard(subAccountUpdated.getNetworkExpirations());
		SubAccount subAccount = this.subAccountRepository.findByOrgShortCodeAndSubAccountShortCodeAndAccountShortCode(
				orgShortCode, subAccountUpdated.getShortCode(), subAccountUpdated.getAccountShortCode());

		if (subAccount != null) {
			subAccount.setUpdatedById(subAccountUpdated.getUpdatedBy());
			subAccount.setUpdatedOn(LocalDateTime.now());
			subAccount.setExpiryDays(daysToExpire);
		}
		else {
			subAccount = new SubAccount();
			subAccount.setAccountId(subAccountUpdated.getAccountId());
			subAccount.setSubAccountId(subAccountUpdated.getSubAccountId());
			subAccount.setOrgShortCode(orgShortCode);
			subAccount.setAccountShortCode(subAccountUpdated.getAccountShortCode());
			subAccount.setSubAccountShortCode(subAccountUpdated.getShortCode());
			subAccount.setCreatedOn(LocalDateTime.now());
			subAccount.setCreatedById(subAccountUpdated.getUpdatedBy());
			subAccount.setHasCustomLogo(DEFAULT_HAS_CUSTOM_LOGO);
			subAccount.setExpiryDays(daysToExpire);
		}
		SubAccountMapper.toEntity(subAccountUpdated, subAccount);

		if (subAccountUpdated.getFundingAccounts() != null) {
			this.updateFundingAccounts(subAccountUpdated.getFundingAccounts(), subAccount.getOrgShortCode(),
					subAccount);
		}
		this.subAccountRepository.save(subAccount);

		this.logUtility.logMessage("subAccount details updated successfully for this given shortCode : "
				+ subAccountUpdated.getShortCode(), INFO, log);
	}

	@Transactional(rollbackOn = Exception.class)
	private void saveOrganization(OrganizationRecord organizationRecord, String org) {
		UUID orgId = UUID.fromString(org);
		try {
			Organization existingOrganization = this.organizationRepository.findByOrgId(orgId);
			if (existingOrganization != null) {

				existingOrganization.setName(organizationRecord.getName());
				existingOrganization.setOrgStatus(OrganizationStatus.ACTIVE.toString());
				existingOrganization.setFaxNumber(organizationRecord.getFaxNumber());
				existingOrganization.setPhoneNumber(organizationRecord.getPhoneNumber());
				existingOrganization.setMarketType(organizationRecord.getMarketType().toString());
				existingOrganization.setContactName(organizationRecord.getContactName());
				existingOrganization.setContactEmail(organizationRecord.getContactEmail());
				existingOrganization.setTin(organizationRecord.getTin());
				existingOrganization.setProgramId(organizationRecord.getProgramId());
				existingOrganization.setOnBoardedOn(organizationRecord.getOnboardedOn().toInstant());
				existingOrganization.setFundingMethod((organizationRecord.getFundingMethod() != null)
						? organizationRecord.getFundingMethod().toString() : null);
				existingOrganization.setClaimOfficePhoneNumber(organizationRecord.getClaimOfficePhoneNumber());
				existingOrganization
					.setClaimOfficePhoneNumberExtension(organizationRecord.getClaimOfficePhoneNumberExtension());
				existingOrganization.setClaimOfficeUrl(organizationRecord.getClaimOfficeUrl());
				existingOrganization.setTechnicalContactName(organizationRecord.getTechnicalContactName());
				existingOrganization.setTechnicalPhoneNumber(organizationRecord.getTechnicalPhoneNumber());
				existingOrganization
					.setTechnicalPhoneNumberExtension(organizationRecord.getTechnicalPhoneNumberExtension());
				existingOrganization.setTechnicalUrl(organizationRecord.getTechnicalUrl());
				existingOrganization.setTechnicalFaxNumber(organizationRecord.getTechnicalFaxNumber());
				existingOrganization.setTechnicalEmailAddress(organizationRecord.getTechnicalEmailAddress());
				existingOrganization.setUpdatedOn(LocalDateTime.now());
				existingOrganization.setType(ORGANIZATION_UPDATED);
				existingOrganization.setUpdatedBy(VIRTUAL_CARD);
				existingOrganization.setOnboardingStatus(organizationRecord.getOnboardingStatus().toString());
				com.uhg.optum.financials.payments.payer.model.Address orgAddressDetails = null;
				if (existingOrganization.getAddressId() != null) {
					orgAddressDetails = this.addressRepository.findById(existingOrganization.getAddressId())
						.orElse(null);
				}
				if (orgAddressDetails != null) {
					orgAddressDetails.setLine1(organizationRecord.getAddress().getAddressLine1());
					orgAddressDetails.setLine2(organizationRecord.getAddress().getAddressLine2());
					orgAddressDetails.setCity(organizationRecord.getAddress().getCity());
					orgAddressDetails.setState(organizationRecord.getAddress().getState());
					orgAddressDetails.setPostalCode(organizationRecord.getAddress().getPostalCode());
					this.addressRepository.save(orgAddressDetails);
					existingOrganization.setAddressId(orgAddressDetails.getId());
					log.debug("updated account Address Details successfully for org: {}",
							organizationRecord.getShortCode());
				}

				this.organizationRepository.save(existingOrganization);
				log.debug("Organization updated having orgId: {}", orgId);
			}
			else {
				Organization organization = new Organization();
				organization.setOrgId(orgId);
				organization.setName(organizationRecord.getName());
				organization.setOrgStatus(OrganizationStatus.ACTIVE.toString());
				organization.setFaxNumber(organizationRecord.getFaxNumber());
				organization.setPhoneNumber(organizationRecord.getPhoneNumber());
				organization.setMarketType(organizationRecord.getMarketType().toString());
				organization.setContactName(organizationRecord.getContactName());
				organization.setContactEmail(organizationRecord.getContactEmail());
				organization.setTin(organizationRecord.getTin());
				organization.setProgramId(organizationRecord.getProgramId());
				organization.setOnBoardedOn(organizationRecord.getOnboardedOn().toInstant());
				organization.setFundingMethod((organizationRecord.getFundingMethod() != null)
						? organizationRecord.getFundingMethod().toString() : null);
				organization.setClaimOfficePhoneNumber(organizationRecord.getClaimOfficePhoneNumber());
				organization
					.setClaimOfficePhoneNumberExtension(organizationRecord.getClaimOfficePhoneNumberExtension());
				organization.setTechnicalContactName(organizationRecord.getTechnicalContactName());
				organization.setTechnicalPhoneNumber(organizationRecord.getTechnicalPhoneNumber());
				organization.setTechnicalPhoneNumberExtension(organizationRecord.getTechnicalPhoneNumberExtension());
				organization.setTechnicalUrl(organizationRecord.getTechnicalUrl());
				organization.setTechnicalFaxNumber(organizationRecord.getTechnicalFaxNumber());
				organization.setTechnicalEmailAddress(organizationRecord.getTechnicalEmailAddress());
				organization.setClaimOfficePhoneNumber(organizationRecord.getClaimOfficePhoneNumber());
				organization
					.setClaimOfficePhoneNumberExtension(organizationRecord.getClaimOfficePhoneNumberExtension());
				organization.setClaimOfficeUrl(organizationRecord.getClaimOfficeUrl());
				organization.setCreatedOn(LocalDateTime.now());
				organization.setOrgShortCode(organizationRecord.getShortCode());
				if (organizationRecord.getAddress() != null) {
					organization.setAddressId(this.getAddress(organizationRecord.getAddress()).getId());
				}
				organization.setClaimOfficeUrl(organizationRecord.getClaimOfficeUrl());
				organization.setType(ORGANIZATION_REGISTERED);
				organization.setCreatedBy(VIRTUAL_CARD);
				organization.setOnboardingStatus(organizationRecord.getOnboardingStatus().toString());
				this.organizationRepository.save(organization);
				log.debug("Created organization with orgId: {}", orgId);
			}
		}
		catch (DataIntegrityViolationException ex) {
			log.error("Data violation while persisting organization information {}", ex.getMessage());
		}
	}

	@Transactional(rollbackOn = Exception.class)
	private void saveAccount(AccountRegistered accountRegistered, String org) {
		UUID orgID = UUID.fromString(org);
		String orgShortCode = this.getOrgShortCode(orgID);
		if (this.accountRepository.existsByAccountShortCodeAndOrgShortCode(accountRegistered.getShortCode(),
				orgShortCode)) {
			this.logUtility.logMessageWithPriority(ACC_EXISTS_ERROR_LOGS + accountRegistered.getShortCode(),
					ValidationConstants.ERROR, log, P3);
		}
		else {
			Account account = toEntity(accountRegistered);
			account.setCreatedOn(LocalDateTime.now());
			account.setCreatedById(accountRegistered.getCreatedBy());
			account.setOrgShortCode(orgShortCode);
			this.accountRepository.save(account);
		}
	}

	@Transactional(rollbackOn = Exception.class)
	public void closeOrganizationDetails(OrganizationClosed dto, UUID orgId) {
		try {
			int number = this.organizationRepository.closeOrganizationById(dto.getUpdatedBy(), orgId);
			if (number == 0) {
				this.logUtility.logMessage("organization does not exist with the given orgId: " + orgId, ERROR, log);
			}
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Error while updating organization details: " + ex.getMessage(), ERROR, log);
		}
	}

	private com.uhg.optum.financials.payments.payer.model.Address getAddress(Address address) {
		com.uhg.optum.financials.payments.payer.model.Address newAddress = new com.uhg.optum.financials.payments.payer.model.Address();
		if (address != null) {
			newAddress.setCity(address.getCity());
			newAddress.setLine1(address.getAddressLine1());
			newAddress.setLine2(address.getAddressLine2());
			newAddress.setState(address.getState());
			newAddress.setPostalCode(address.getPostalCode());
			newAddress.setCountry("us");
			return newAddress;
		}
		return null;
	}

	private Integer getExpiryDaysForVirtualCard(NetworkExpiration[] networkExpirations) {
		if (networkExpirations == null) {
			return null;
		}
		return Arrays.stream(networkExpirations)
			.filter((networkExpiration) -> networkExpiration.getNetworkId().equalsIgnoreCase(VIRTUAL_CARD_NETWORK_ID))
			.map(NetworkExpiration::getDaysToExpire)
			.findFirst()
			.orElse(null);
	}

	private boolean checkIfOrgExists(UUID orgId) {
		if (!this.organizationRepository.existsByOrgId(orgId)) {
			this.logUtility.logMessageWithPriority(ORG_NOT_FOUND_ERROR_LOGS + getOrgShortCode(orgId),
					ValidationConstants.ERROR, log, P3);
			return false;
		}
		else {
			return true;
		}

	}

	private String getOrgShortCode(UUID orgId) {
		return this.organizationRepository.findByOrgId(orgId).getOrgShortCode();
	}

	private boolean checkOrgExistElseRaiseP3(UUID orgId) {
		if (this.organizationRepository.existsByOrgId(orgId)) {
			return true;
		}
		else {
			this.logUtility.logMessageWithPriority(ORG_NOT_FOUND_ID_ERROR_LOGS + orgId, ValidationConstants.ERROR, log,
					P3);
			return false;
		}
	}

	private boolean isAccountExist(String accountShortCode, String orgShortCode) {
		return this.accountRepository.existsByAccountShortCodeAndOrgShortCode(accountShortCode, orgShortCode);
	}

	private boolean isSubAccountExists(String accountShortCode, String subAccountShortCode, String orgShortCode) {
		return this.subAccountRepository.existsByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(accountShortCode,
				subAccountShortCode, orgShortCode);
	}

	private void updateFundingAccounts(FundingAccounts fundingAccounts, String orgShortCode, SubAccount subAccount) {
		if (fundingAccounts.getFundingForeignAccount() != null) {
			getForeignFundingAccount(fundingAccounts.getFundingForeignAccount(), orgShortCode);
			subAccount.setFundingForeignAccount(fundingAccounts.getFundingForeignAccount());
		}
		if (fundingAccounts.getUnloadForeignAccount() != null) {
			getForeignFundingAccount(fundingAccounts.getUnloadForeignAccount(), orgShortCode);
			subAccount.setUnloadForeignAccount(fundingAccounts.getUnloadForeignAccount());
		}
		if (fundingAccounts.getPayFromBankAccount() != null) {
			getForeignFundingAccount(fundingAccounts.getPayFromBankAccount(), orgShortCode);
			subAccount.setPayFromBankAccount(fundingAccounts.getPayFromBankAccount());
		}
	}

	private void getForeignFundingAccount(UUID id, String orgShortCode) {

		if (this.bankAccountRepository.findByBankAccountId(id) == null) {
			BankAccount bankAccountDetails = new BankAccount();
			EncryptedBankAccountDto encryptedBankAccountDto = getBankAccountById(this.encryptedBankAccountEndpoint, id,
					orgShortCode, EncryptedBankAccountDto.class);
			bankAccountDetails.setBankAccountId(id);
			bankAccountDetails.setOrgShortCode(orgShortCode);
			bankAccountDetails.setRoutingNumber(encryptedBankAccountDto.getRoutingNumber());
			bankAccountDetails.setAccountType(String.valueOf(encryptedBankAccountDto.getAccountType()));
			bankAccountDetails.setVerificationStatus(String.valueOf(encryptedBankAccountDto.getVerificationStatus()));
			bankAccountDetails.setType("bank_account_created");
			bankAccountDetails.setCreatedBy("sub_account_updated");
			bankAccountDetails.setCreatedOn(LocalDateTime.now());
			bankAccountDetails.setOrgId(this.organizationRepository.findByOrgShortCode(orgShortCode).getOrgId());
			String accountNumber = encryptedBankAccountDto.getAccountNumber();
			String lastFourDigits = (accountNumber.length() > 4) ? accountNumber.substring(accountNumber.length() - 4)
					: accountNumber;
			bankAccountDetails.setAccountNumberLastFourDigits(lastFourDigits);
			// Encrypt the account number before saving it.
			EncryptedData encryptedData = this.encryptionAtRestService.encrypt(accountNumber);
			if (encryptedData == null) {
				this.logUtility.logMessage("Error encrypting account number at rest for Bank Account Id: " + id,
						ValidationConstants.ERROR, log);
				throw new BankAccountException("Error encrypting account number at rest for Bank Account Id: " + id);
			}
			bankAccountDetails.setAccountNumber(encryptedData.getCipherText());
			bankAccountDetails.setEncryptionAtRestVersion(encryptedData.getKeyVersion());
			this.bankAccountRepository.save(bankAccountDetails);

		}

	}

	@Getter
	@Builder
	private static final class OrganizationRecord {

		@Getter
		private UUID id;

		@Getter
		private MarketType marketType;

		@Getter
		private String orgStatus;

		@Getter
		private String name;

		@Getter
		private Address address;

		@Getter
		private String faxNumber;

		@Getter
		private String phoneNumber;

		@Getter
		private String contactEmail;

		@Getter
		private String tin;

		@Getter
		private Date onboardedOn;

		@Getter
		private String programId;

		@Getter
		private FundingMethod fundingMethod;

		@Getter
		private String claimOfficePhoneNumber;

		@Getter
		private String claimOfficePhoneNumberExtension;

		@Getter
		private String claimOfficeUrl;

		@Getter
		private String technicalContactName;

		@Getter
		private String technicalPhoneNumber;

		@Getter
		private String technicalPhoneNumberExtension;

		@Getter
		private String technicalUrl;

		@Getter
		private String technicalFaxNumber;

		@Getter
		private String technicalEmailAddress;

		@Getter
		private String shortCode;

		@Getter
		private OnboardingStatus onboardingStatus;

		@Getter
		private String contactName;

	}

}
