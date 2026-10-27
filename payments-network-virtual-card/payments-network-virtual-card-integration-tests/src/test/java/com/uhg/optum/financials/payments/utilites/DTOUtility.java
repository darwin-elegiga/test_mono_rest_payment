package com.uhg.optum.financials.payments.utilites;

import com.uhg.optum.financials.payments.APITrigger.APIEndPointsImpl;
import com.uhg.optum.financials.payments.bankaccount.enums.AccountType;
import com.uhg.optum.financials.payments.bankaccount.enums.PayerBankAccountVerificationStatus;
import com.uhg.optum.financials.payments.bankaccount.events.BankAccountCreated;
import com.uhg.optum.financials.payments.organization.enums.FundingMethod;
import com.uhg.optum.financials.payments.organization.enums.MarketType;
import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.organization.events.OrganizationRegistered;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.organization.events.SubAccountUpdated;
import com.uhg.optum.financials.payments.organization.models.Address;
import com.uhg.optum.financials.payments.organization.models.Contact;
import com.uhg.optum.financials.payments.organization.models.FundingAccounts;
import com.uhg.optum.financials.payments.organization.models.SecondaryContact;
import com.uhg.optum.financials.payments.payer.dto.AccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.SubAccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.SubAccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.dto.bankaccount.BankAccountEventDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchDetailDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchEventDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchNetworkDto;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationDTO;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationRegisteredEvent;
import com.uhg.optum.financials.shared.client.constant.PayeeType;
import com.uhg.optum.financials.shared.client.constant.PaymentType;
import com.uhg.optum.financials.shared.client.constant.RemitMethod;
import com.uhg.optum.financials.shared.client.dto.payment.AddressDto;
import com.uhg.optum.financials.shared.client.dto.payment.ClaimsDto;
import com.uhg.optum.financials.shared.client.dto.payment.DocumentDto;
import com.uhg.optum.financials.shared.client.dto.payment.PayeeDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.dto.payment.ShippingDetailsDto;
import integration.dto.BankAccountNonProd;
import jakarta.validation.constraints.NotNull;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.ACCOUNT_REGISTERED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.ORGANIZATION_REGISTERED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.SUB_ACCOUNT_REGISTERED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.SUB_ACCOUNT_UPDATED;

public class DTOUtility extends Helper {

	private static final Logger log = LoggerFactory.getLogger(DTOUtility.class);

	public PaymentRequestDto paymentRequestDto = null;

	static APIEndPointsImpl apiEndPointsImpl = APIEndPointsImpl.getAPIEndPointsImplInstance();

	public static Map<String, AccountRegistered> scenarioPayerMap = new HashMap<>();

	public static Map<String, SubAccountRegistered> scenarioSubPayerMap = new HashMap<>();

	public static Map<String, String> scenarioPaymentNumMap = new HashMap<>();

	// ----------------------
	// Address helpers (valid)
	// ----------------------
	public AddressDto setAddress() {
		AddressDto address = new AddressDto();
		address.setLine1("8800 TERRENE CT STE 103");
		address.setLine2("P.O. BOX 740800");
		address.setCity("BONITA SPRINGS");
		address.setState("FL");
		address.setCountry("USA");
		address.setPostalCode("34135");
		return address;
	}

	public Address setOrgAddress() {
		Address orgAddressDto = new Address();
		orgAddressDto.setAddressLine1("RICHARDSON/SPRGFLD SRVC CNTR");
		orgAddressDto.setAddressLine2("P.O. BOX 740800");
		orgAddressDto.setCity("ATLANTA");
		orgAddressDto.setState("GA");
		orgAddressDto.setPostalCode("303740800");
		return orgAddressDto;
	}

	public Contact getContact() {
		Contact contact = new Contact();
		contact.setContactName("Legolas");
		contact.setEmailAddress("fellowship@gmail.com");
		contact.setPhoneNumber("3567134879");
		contact.setFaxNumber("9876545678");
		return contact;
	}

	public SecondaryContact getClaimOfficeContact() {
		SecondaryContact secondaryContact = new SecondaryContact();
		secondaryContact.setPhoneNumber("756483924");
		secondaryContact.setPhoneNumberExtension("1234");
		secondaryContact.setUrl("ClaimOfficeUrl.org.com");
		return secondaryContact;
	}

	public SecondaryContact getTechnicalContact() {
		SecondaryContact secondaryContact = new SecondaryContact();
		secondaryContact.setContactName("John");
		secondaryContact.setEmailAddress("John@gmail.com");
		secondaryContact.setPhoneNumber("8876545678");
		secondaryContact.setPhoneNumberExtension("234");
		secondaryContact.setUrl("tech.org.com");
		return secondaryContact;
	}

	// --------------------------------------
	// Payment request builder (baseline valid)
	// --------------------------------------
	public PaymentRequestDto getPaymentRequest(String orgShortCode, String accountShortCode,
			String subAccountShortCode) {
		this.paymentRequestDto = new PaymentRequestDto();
		this.paymentRequestDto.setOrigin("Issuing");
		this.paymentRequestDto.setOrgShortCode(orgShortCode);
		this.paymentRequestDto.setAccountShortCode(accountShortCode);
		this.paymentRequestDto.setSubAccountShortCode(subAccountShortCode);
		this.paymentRequestDto.setCorrelationId(generateRandomAlphaNumericString(15));

		// Payment details – valid per DTO constraints
		PaymentDetailsDto paymentDetails = getPaymentDetailsDto();
		this.paymentRequestDto.setPaymentDetails(paymentDetails);

		// Payee details – valid
		PayeeDetailsDto payeeDetails = new PayeeDetailsDto();
		payeeDetails.setType(PayeeType.B.name());
		payeeDetails.setName("United HealthCare Services, Inc.");
		payeeDetails.setAddress(setAddress());
		payeeDetails.setTaxId(createRandomNumber(9)); // 9 digits numeric
		payeeDetails.setNpi(createRandomNumber(10)); // 10 chars
		payeeDetails.setReceiverId(createRandomNumber(8)); // 1..50 chars
		this.paymentRequestDto.setPayeeDetails(payeeDetails);

		// Shipping details – optional, but valid values
		ShippingDetailsDto shippingDetails = new ShippingDetailsDto();
		shippingDetails.setName("A"); // within 1..50
		shippingDetails.setAddress(setAddress());
		this.paymentRequestDto.setShippingDetails(shippingDetails);

		// Claims baseline (upper env compliant)
		ClaimsDto claimsDto = new ClaimsDto();
		claimsDto.setTotalCount(0);
		claimsDto.setUrl(null); // null/empty OK when count==0 and PLB=false
		claimsDto.setDocType("Claims");
		claimsDto.setPlb(false); // PLB false to satisfy baseline rule
		this.paymentRequestDto.setClaims(claimsDto);

		// Documents – use a valid URL to avoid doc validation issues
		DocumentDto documentDto = new DocumentDto();
		documentDto.setUrl("https://example.com/doc.pdf");
		documentDto.setDocType("pdf");
		List<DocumentDto> documentDtoList = new ArrayList<>();
		documentDtoList.add(documentDto);
		this.paymentRequestDto.setDocuments(documentDtoList);

		return this.paymentRequestDto;
	}

	@NotNull
	private static PaymentDetailsDto getPaymentDetailsDto() {
		PaymentDetailsDto paymentDetails = new PaymentDetailsDto();
		paymentDetails.setId(getUniquePaymentNumber());
		paymentDetails.setIssuerPaymentId(getUniquePaymentNumber());
		paymentDetails.setReferenceId(generateRandomAlphabetsString(7));
		String date = String.valueOf(LocalDate.now());
		paymentDetails.setPaymentDate(date);
		paymentDetails.setAdjudicationCutoffDate("2024-10-23");
		paymentDetails.setAdjudicationVersion("34");
		paymentDetails.setAmount(BigDecimal.valueOf(44.99)); // decimal with 2 precision
		paymentDetails.setRemitMethod(RemitMethod.PDF.name());
		paymentDetails.setJurisdictionalBody("AB");
		paymentDetails.setNumberOfAttachedPdfs(4);
		paymentDetails.setSettlementTime("000000");
		paymentDetails.setType(PaymentType.PAYMENT);
		paymentDetails.setPin("TEST-PIN-123");
		return paymentDetails;
	}

	// ------------------------------------------------------
	// Scenario-based request creation (ensure claims valid)
	// ------------------------------------------------------
	public static void createAndLoadPaymentRequest(String orgShortCode, String accountShortCode,
			String subAccountShortCode, String scenario) throws IOException {

		DTOUtility dtoUtility = new DTOUtility();
		PaymentRequestDto paymentRequestDto;

		switch (scenario) {
			case "VALID" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
			}
			case "BPR_I" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setAmount(BigDecimal.valueOf(100.00));
				paymentRequestDto.getPaymentDetails().setType(PaymentType.PAYMENT);
			}
			case "BPR_H_Notification" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setAmount(BigDecimal.valueOf(0));
				paymentRequestDto.getPaymentDetails().setType(PaymentType.NOTIFICATION);
			}
			case "BPR_H_PaymentOnly" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setAmount(BigDecimal.valueOf(0));
				paymentRequestDto.getPaymentDetails().setType(PaymentType.PAYMENT_ONLY);
			}
			case "BPR_D" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setAmount(BigDecimal.valueOf(44.99));
				paymentRequestDto.getPaymentDetails().setType(PaymentType.PAYMENT_ONLY);
			}
			case "SETTLEMENT_000001" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setSettlementTime("000001");
			}
			case "SETTLEMENT_000002" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setSettlementTime("000002");
			}
			case "SETTLEMENT_000003" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setSettlementTime("000003");
			}
			case "ADJ_VS_VALID" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setAdjudicationVersion("34");
			}
			case "ADJ_VS_NULL" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setAdjudicationVersion(null);
			}
			case "ADJ_VS_EMPTY" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setAdjudicationVersion("");
			}
			case "ADJ_CUT_OFF_DATE_VALID" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setAdjudicationCutoffDate("2024-10-23");
			}
			case "ADJ_CUT_OFF_DATE_NULL" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setAdjudicationCutoffDate(null);
			}
			case "ADJ_CUT_OFF_DATE_EMPTY" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPaymentDetails().setAdjudicationCutoffDate("");
			}
			case "ADDRESS_WITH_LINE2" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPayeeDetails().getAddress().setLine2("P.O. BOX 740800");
			}
			case "ADDRESS_WITHOUT_LINE2" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPayeeDetails().getAddress().setLine2("");
			}
			case "NPI_Available" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPayeeDetails().setNpi(createRandomNumber(10));
			}
			case "NPI_Not_available" -> {
				paymentRequestDto = dtoUtility.getPaymentRequest(orgShortCode, accountShortCode, subAccountShortCode);
				paymentRequestDto.getPayeeDetails().setNpi(null);
			}
			default -> throw new IllegalArgumentException("Invalid scenario: " + scenario);
		}

		// Baseline claims must remain valid
		ensureValidClaimsBaseline(paymentRequestDto);

		scenarioPaymentNumMap.put(scenario, paymentRequestDto.getPaymentDetails().getId());
		apiEndPointsImpl.pushPayment(paymentRequestDto);
	}

	public static PaymentRequestDto createAndLoadPaymentRequestForClaim(String orgId, String accountId,
			String subAccountId, String scenario, String ClaimURL) throws IOException {
		DTOUtility dtoUtility = new DTOUtility();
		PaymentRequestDto paymentRequestDto = dtoUtility.getPaymentRequest(orgId, accountId, subAccountId);

		// Baseline normalize first
		ensureValidClaimsBaseline(paymentRequestDto);

		// Apply scenario-specific claim rules
		switch (scenario) {
			case "COMPLETE_CLAIM", "EMPTY_CLAIM", "MANDATORY" -> {
				paymentRequestDto.getClaims().setTotalCount(1);
				paymentRequestDto.getClaims().setPlb(false);
				paymentRequestDto.getClaims()
					.setUrl((ClaimURL != null && !ClaimURL.isBlank()) ? ClaimURL : "https://example.com/doc");
			}
			case "PER_CLAIM" -> {
				paymentRequestDto.getClaims().setTotalCount(8);
				paymentRequestDto.getClaims().setPlb(false);
				paymentRequestDto.getClaims()
					.setUrl((ClaimURL != null && !ClaimURL.isBlank()) ? ClaimURL : "https://example.com/doc");
			}
			case "PLB_FALSE" -> {
				paymentRequestDto.getClaims().setPlb(false);
				if (paymentRequestDto.getClaims().getTotalCount() == null
						|| paymentRequestDto.getClaims().getTotalCount() == 0) {
					paymentRequestDto.getClaims().setTotalCount(0);
					paymentRequestDto.getClaims().setUrl(null);
				}
				else {
					paymentRequestDto.getClaims()
						.setUrl((ClaimURL != null && !ClaimURL.isBlank()) ? ClaimURL : "https://example.com/doc");
				}
			}
			default -> throw new IllegalArgumentException("Invalid scenario: " + scenario);
		}

		scenarioPaymentNumMap.put(scenario, paymentRequestDto.getPaymentDetails().getId());
		apiEndPointsImpl.pushPayment(paymentRequestDto);
		return paymentRequestDto;
	}

	// Keep claims baseline valid across builders
	private static void ensureValidClaimsBaseline(PaymentRequestDto dto) {
		if (dto.getClaims() == null) {
			dto.setClaims(new ClaimsDto());
		}
		dto.getClaims().setTotalCount(0);
		dto.getClaims().setPlb(false);
		dto.getClaims().setUrl(null);
	}

	// -----------------------------
	// Payer & Organization builders
	// -----------------------------
	public AccountRegisteredEvent loadPayer(UUID orgId) {
		AccountRegisteredEvent payerDto = new AccountRegisteredEvent();
		AccountRegistered accountRegistered = getAccountRegistered();
		payerDto.setId(orgId);
		payerDto.setTenantId(UUID.randomUUID());
		payerDto.setEventId(UUID.randomUUID());
		payerDto.setAccountRegistered(accountRegistered);
		return payerDto;
	}

	public AccountRegistered getAccountRegistered() {
		AccountRegistered accountRegistered = new AccountRegistered();
		accountRegistered.setAccountId(UUID.randomUUID());
		accountRegistered.setMarketType(MarketType.MEDICAL);
		accountRegistered.setName("Acc" + generateRandomAlphabetsString(6));
		accountRegistered.setShortCode(generateRandomAlphabetsString(10).toUpperCase());
		accountRegistered.setAddress(setOrgAddress());
		accountRegistered.setFaxNumber("45645645");
		accountRegistered.setPhoneNumber("1345345623");
		accountRegistered.setContactEmail("contactEmail@gmail.com");
		accountRegistered.setTin(createRandomNumber(9));
		accountRegistered.setHoldAndRelease(true);
		accountRegistered.setProgramId("HEALTHCARE");
		accountRegistered.setPayer835Id(generateRandomAlphabetsString(5).toUpperCase());
		accountRegistered.setClaimOfficePhoneNumber(createRandomNumber(10));
		accountRegistered.setContactName("Bob Smith");
		accountRegistered.setClaimOfficeUrl("https://claimOfficeUrl.com");
		accountRegistered.setTechnicalContactName("TechnicalContactName");
		accountRegistered.setTechnicalPhoneNumber(createRandomNumber(10));
		accountRegistered.setTechnicalUrl("https://TechnicalUrl.com");
		accountRegistered.setTechnicalFaxNumber(createRandomNumber(10));
		accountRegistered.setTechnicalEmailAddress("TechnicalEmail@optum.com");
		accountRegistered.setType(ACCOUNT_REGISTERED);
		return accountRegistered;
	}

	public OrganizationRegisteredEvent loadOrganization() {
		OrganizationRegisteredEvent organizationEventDto = new OrganizationRegisteredEvent();
		organizationEventDto.setId(UUID.randomUUID());
		organizationEventDto.setTenantId(UUID.randomUUID());
		organizationEventDto.setEventId(UUID.randomUUID());
		OrganizationRegistered organizationRegistered = getOrganizationRegistered();
		organizationEventDto.setOrganizationRegistered(organizationRegistered);
		return organizationEventDto;
	}

	public OrganizationRegistered getOrganizationRegistered() {
		OrganizationRegistered organizationRegistered = new OrganizationRegistered();
		organizationRegistered.setMarketType(MarketType.MEDICAL);
		organizationRegistered.setName("Org" + generateRandomAlphabetsString(6));
		organizationRegistered.setShortCode(generateRandomAlphabetsString(10).toUpperCase());
		organizationRegistered.setAddress(setOrgAddress());
		organizationRegistered.setTin(createRandomNumber(9));
		organizationRegistered.setOnboardedOn(new Date());
		organizationRegistered.setProgramId("HEALTHCARE");
		organizationRegistered.setFundingMethod(FundingMethod.NON_FUNDED);
		organizationRegistered.setType(ORGANIZATION_REGISTERED);
		organizationRegistered.setContact(getContact());
		organizationRegistered.setClaimOfficeContact(getClaimOfficeContact());
		organizationRegistered.setTechnicalContact(getTechnicalContact());
		return organizationRegistered;
	}

	public OrganizationDTO getOrgRegistered() {
		OrganizationDTO organizationRegistered = new OrganizationDTO();
		organizationRegistered.setMarketType(MarketType.MEDICAL);
		organizationRegistered.setName("Org" + generateRandomAlphabetsString(6));
		organizationRegistered.setShortCode(generateRandomAlphabetsString(10).toUpperCase());
		organizationRegistered.setAddress(setOrgAddress());
		organizationRegistered.setTin("654367895");
		organizationRegistered.setOnboardedOn(new Date());
		organizationRegistered.setProgramName("HEALTHCARE");
		organizationRegistered.setFundingMethod(FundingMethod.NON_FUNDED);
		organizationRegistered.setType(ORGANIZATION_REGISTERED);
		organizationRegistered.setContact(getContact());
		organizationRegistered.setClaimOfficeContact(getClaimOfficeContact());
		organizationRegistered.setTechnicalContact(getTechnicalContact());
		return organizationRegistered;
	}

	public SubAccountRegisteredEvent loadSubAccount(UUID orgId, String accountShortCode, UUID bankId) {
		SubAccountRegisteredEvent subPayerEventDto = new SubAccountRegisteredEvent();
		SubAccountRegistered subAccountRegistered = getSubAccountRegistered(bankId);
		subAccountRegistered.setAccountShortCode(accountShortCode);
		subPayerEventDto.setId(orgId);
		subPayerEventDto.setTenantId(UUID.randomUUID());
		subPayerEventDto.setEventId(UUID.randomUUID());
		subPayerEventDto.setSubAccountRegistered(subAccountRegistered);
		return subPayerEventDto;
	}

	public SubAccountRegistered getSubAccountRegistered(UUID bankid) {
		SubAccountRegistered subAccountRegistered = new SubAccountRegistered();
		subAccountRegistered.setAccountId(UUID.randomUUID());
		subAccountRegistered.setSubAccountId(UUID.randomUUID());
		subAccountRegistered.setShortCode(generateRandomAlphabetsString(10).toUpperCase());
		subAccountRegistered.setMarketType(MarketType.MEDICAL);
		subAccountRegistered.setName("SubAcc" + generateRandomAlphabetsString(6));
		subAccountRegistered.setAddress(setOrgAddress());
		subAccountRegistered.setFaxNumber(createRandomNumber(10));
		subAccountRegistered.setPhoneNumber(createRandomNumber(10));
		subAccountRegistered.setContactEmail("subPayer@optum.com");
		subAccountRegistered.setTin(createRandomNumber(9));
		subAccountRegistered.setHoldAndRelease(true);
		subAccountRegistered.setProgramId("HEALTHCARE");
		subAccountRegistered.setContactName("Bob Smith");
		subAccountRegistered.setPayer835Id(generateRandomAlphabetsString(5).toUpperCase());
		subAccountRegistered.setClaimOfficePhoneNumber(createRandomNumber(10));
		subAccountRegistered.setClaimOfficePhoneNumberExtension("771");
		subAccountRegistered.setClaimOfficeUrl("https://claimOfficeUrl.com");
		subAccountRegistered.setTechnicalContactName("TechnicalContactName");
		subAccountRegistered.setTechnicalPhoneNumber(createRandomNumber(10));
		subAccountRegistered.setTechnicalPhoneNumberExtension("741");
		subAccountRegistered.setTechnicalUrl("https://TechnicalUrl.com");
		subAccountRegistered.setTechnicalFaxNumber(createRandomNumber(10));
		subAccountRegistered.setTechnicalEmailAddress("TechnicalEmail@optum.com");
		subAccountRegistered.setType(SUB_ACCOUNT_REGISTERED);
		subAccountRegistered.setReturnMailingAddress(setOrgAddress());

		FundingAccounts fundingAccounts = new FundingAccounts();
		fundingAccounts.setFundingForeignAccount(bankid);
		fundingAccounts.setUnloadForeignAccount(bankid);
		fundingAccounts.setFundingLocalAccount(bankid);
		fundingAccounts.setUnloadLocalAccount(bankid);
		fundingAccounts.setPayFromBankAccount(bankid);

		subAccountRegistered.setFundingAccounts(fundingAccounts);
		return subAccountRegistered;
	}

	public SubAccountUpdatedDto loadSubAccountUpdated(UUID orgId, String accountShortCode, UUID bankId,
			String subAccountShortCode) {
		SubAccountUpdatedDto subPayerEventDto = new SubAccountUpdatedDto();
		SubAccountUpdated subAccountUpdated = getSubAccountUpdated(bankId);
		subAccountUpdated.setAccountShortCode(accountShortCode.toUpperCase());
		subAccountUpdated.setShortCode(subAccountShortCode.toUpperCase());
		subAccountUpdated.setReturnMailingAddress(setOrgAddress());
		subPayerEventDto.setId(orgId);
		subPayerEventDto.setSubAccountUpdated(subAccountUpdated);
		return subPayerEventDto;
	}

	public SubAccountUpdated getSubAccountUpdated(UUID bankid) {
		SubAccountUpdated subAccountUpdated = new SubAccountUpdated();
		subAccountUpdated.setAccountId(UUID.randomUUID());
		subAccountUpdated.setSubAccountId(UUID.randomUUID());
		subAccountUpdated.setMarketType(MarketType.MEDICAL);
		subAccountUpdated.setName("SubAcc" + generateRandomAlphabetsString(6));
		subAccountUpdated.setAddress(setOrgAddress());
		subAccountUpdated.setFaxNumber(createRandomNumber(10));
		subAccountUpdated.setPhoneNumber(createRandomNumber(10));
		subAccountUpdated.setContactEmail("subPayer@optum.com");
		subAccountUpdated.setTin(createRandomNumber(9));
		subAccountUpdated.setHoldAndRelease(true);
		subAccountUpdated.setProgramId("HEALTHCARE");
		subAccountUpdated.setContactName("Bob Smith");
		subAccountUpdated.setPayer835Id(generateRandomAlphabetsString(5).toUpperCase());
		subAccountUpdated.setClaimOfficePhoneNumber(createRandomNumber(10));
		subAccountUpdated.setClaimOfficePhoneNumberExtension("771");
		subAccountUpdated.setClaimOfficeUrl("https://claimOfficeUrl.com");
		subAccountUpdated.setTechnicalContactName("TechnicalContactName");
		subAccountUpdated.setTechnicalPhoneNumber(createRandomNumber(10));
		subAccountUpdated.setTechnicalPhoneNumberExtension("741");
		subAccountUpdated.setTechnicalUrl("https://TechnicalUrl.com");
		subAccountUpdated.setTechnicalFaxNumber(createRandomNumber(10));
		subAccountUpdated.setTechnicalEmailAddress("TechnicalEmail@optum.com");
		subAccountUpdated.setType(SUB_ACCOUNT_UPDATED);
		subAccountUpdated.setFundingMethod(FundingMethod.PRE_FUNDED_NEXT_DAY);
		subAccountUpdated.setReturnMailingAddress(setOrgAddress());
		FundingAccounts fundingAccounts = new FundingAccounts();
		fundingAccounts.setFundingForeignAccount(bankid);
		fundingAccounts.setUnloadForeignAccount(bankid);
		fundingAccounts.setFundingLocalAccount(bankid);
		fundingAccounts.setUnloadLocalAccount(bankid);
		subAccountUpdated.setFundingAccounts(fundingAccounts);

		return subAccountUpdated;
	}

	public BatchEventDto getBatchEventDto(String orgShortCode, String correlationId, Long paymentCount,
			BigDecimal amount) {
		BatchEventDto batchEventDto = new BatchEventDto();
		batchEventDto.setId(UUID.randomUUID().toString());
		batchEventDto.setTenantId(UUID.randomUUID());
		batchEventDto.setEventId(UUID.randomUUID());
		batchEventDto.setCorrelationDto(getCorrelationDto(orgShortCode, correlationId, paymentCount, amount));
		return batchEventDto;
	}

	public CorrelationDto getCorrelationDto(String orgShortCode, String correlationId, Long payment,
			BigDecimal amount) {

		List<BatchDetailDto> batches = new ArrayList<>();

		List<BatchNetworkDto> networkDtos = new ArrayList<>();
		BatchNetworkDto batchNetworkDto = new BatchNetworkDto();
		batchNetworkDto.setNetwork("VIRTUAL_CARD");
		batchNetworkDto.setNumberOfPayments(payment);
		batchNetworkDto.setTotalAmount(amount);
		networkDtos.add(batchNetworkDto);

		BatchDetailDto batchDetailDto = new BatchDetailDto();
		batchDetailDto.setBatchId(createRandomNumber(10));
		batchDetailDto.setNetworks(networkDtos);
		batches.add(batchDetailDto);

		CorrelationDto correlationDto = new CorrelationDto();
		correlationDto.setOrg(orgShortCode);
		correlationDto.setCorrelationId(correlationId);
		correlationDto.setNumberOfBatches(1);
		correlationDto.setBatches(batches);
		correlationDto.setType("batch_correlated");

		return correlationDto;
	}

	public BankAccountEventDto bankAccountEventDto(UUID orgId) {
		BankAccountEventDto bankAccountEventDto = new BankAccountEventDto();
		bankAccountEventDto.setId(UUID.randomUUID());
		bankAccountEventDto.setTenantId(UUID.randomUUID());
		bankAccountEventDto.setEventId(UUID.randomUUID());

		BankAccountCreated bankAccountCreated = new BankAccountCreated();
		bankAccountCreated.setName("Optum Bank");
		bankAccountCreated.setDisplayName("OB 6738");
		bankAccountCreated.setAccountType(AccountType.CHECKING);
		bankAccountCreated.setRoutingNumber(createRandomNumber(9));
		bankAccountCreated.setAccountNumberLastFourDigits("6738");
		bankAccountCreated.setOrganizationId(orgId);
		bankAccountCreated.setVerificationStatus(PayerBankAccountVerificationStatus.PENDING);
		bankAccountCreated.setType("bank_account_created");
		bankAccountEventDto.setBankAccountCreated(bankAccountCreated);
		return bankAccountEventDto;
	}

	public BankAccountNonProd getBankAccountNonProd() {
		BankAccountNonProd bankAccount = new BankAccountNonProd();
		bankAccount.setName(generateRandomAlphabetsString(6) + " Bank");
		bankAccount.setDisplayName("OB 3210");
		bankAccount.setAccountType("Checking");
		bankAccount.setRoutingNumber(createRandomNumber(9));
		bankAccount.setAccountNumber(createRandomNumber(12));
		return bankAccount;
	}

}
