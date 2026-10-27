package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.CardHolderDto;
import com.uhg.optum.financials.payments.payer.dto.CreateCardRequest;
import com.uhg.optum.financials.payments.payer.dto.Expiry;
import com.uhg.optum.financials.payments.payer.exception.CardProcessingError;
import com.uhg.optum.financials.payments.payer.model.Bin;
import com.uhg.optum.financials.payments.payer.model.BillingAddress;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.CardHolder;
import com.uhg.optum.financials.payments.payer.model.SpendRules;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.repository.BinRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.SpendRulesConfigRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.ConsentDecisionService;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.CardOperationLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.WebhookAlertUtil;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import jakarta.validation.Validator;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.springframework.dao.IncorrectResultSizeDataAccessException;
import org.springframework.test.util.ReflectionTestUtils;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.Optional;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.BDDMockito.given;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.RETURNS_DEEP_STUBS;
import static org.mockito.Mockito.mock;

class VirtualCardCreateServiceImplTests {

	@Mock
	private LogUtility logUtility;

	@Mock
	private ZetaApiService zetaApiService;

	@Mock
	private CardCreationRepository cardCreationRepository;

	@Mock
	private BinRepository binRepository;

	@Mock
	private SubAccountRepository subAccountRepository;

	@Mock
	private SpendRulesConfigRepository spendRulesConfigRepository;

	@Mock
	private ConsentDecisionService consentDecisionService;

	@Mock
	private AuditLogger auditLogger;

	@Mock
	private CardOperationLogger cardOperationLogger;

	@InjectMocks
	private VirtualCardCreateServiceImpl service;

	@Mock
	private WebhookAlertUtil webhookAlertUtil;

	@Mock
	private Validator validator;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
		ReflectionTestUtils.setField(this.service, "spendRulesConfigRepository", this.spendRulesConfigRepository);
		ReflectionTestUtils.setField(this.service, "logUtility", this.logUtility);
		ReflectionTestUtils.setField(this.service, "subAccountRepository", this.subAccountRepository);
		ReflectionTestUtils.setField(this.service, "binRepository", this.binRepository);
		ReflectionTestUtils.setField(this.service, "cardCreationRepository", this.cardCreationRepository);
		ReflectionTestUtils.setField(this.service, "consentDecisionService", this.consentDecisionService);
		ReflectionTestUtils.setField(this.service, "webhookAlertUtil", this.webhookAlertUtil);
	}

	@Test
	@DisplayName("buildCardCreationRequest should set autoActivateCard=false when consent service returns false")
	void buildCardCreationRequestShouldSetAutoActivateCardFalseWhenConsentServiceReturnsFalse() {
		PaymentRequestDto event = this.createValidBuildRequestEvent();
		CardHolderDto cardHolder = CardHolderDto.builder().build();
		LocalDate expiryDate = LocalDate.of(2025, 12, 31);
		SpendRules spendRules = new SpendRules();
		spendRules.setRuleCode(new ArrayList<>());

		given(this.consentDecisionService.determineAutoActivateCard(event)).willReturn(false);
		given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACC", "SUB",
				"ORG"))
			.willReturn(Optional.of(this.createSubAccount("FUND")));
		given(this.binRepository.findByFundingType("FUND")).willReturn(Optional.of(this.createBin(123456L)));

		CreateCardRequest request = this.service.buildCardCreationRequest(event, cardHolder, expiryDate, spendRules);

		assertThat(request.isAutoActivateCard()).isFalse();
	}

	@Test
	@DisplayName("buildCardCreationRequest should set autoActivateCard=true when consent service returns true")
	void buildCardCreationRequestShouldSetAutoActivateCardTrueWhenConsentServiceReturnsTrue() {
		PaymentRequestDto event = this.createValidBuildRequestEvent();
		CardHolderDto cardHolder = CardHolderDto.builder().build();
		LocalDate expiryDate = LocalDate.of(2025, 12, 31);
		SpendRules spendRules = new SpendRules();
		spendRules.setRuleCode(new ArrayList<>());

		given(this.consentDecisionService.determineAutoActivateCard(event)).willReturn(true);
		given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACC", "SUB",
				"ORG"))
			.willReturn(Optional.of(this.createSubAccount("FUND")));
		given(this.binRepository.findByFundingType("FUND")).willReturn(Optional.of(this.createBin(123456L)));

		CreateCardRequest request = this.service.buildCardCreationRequest(event, cardHolder, expiryDate, spendRules);

		assertThat(request.isAutoActivateCard()).isTrue();
	}

	@Test
	@DisplayName("should build card creation request successfully")
	void shouldBuildCardCreationRequestSuccessfully() {
		// given
		PaymentRequestDto event = mock(PaymentRequestDto.class, RETURNS_DEEP_STUBS);
		given(event.getAccountShortCode()).willReturn("ACC");
		given(event.getSubAccountShortCode()).willReturn("SUB");
		given(event.getOrgShortCode()).willReturn("ORG");
		given(event.getPaymentDetails().getAmount()).willReturn(BigDecimal.valueOf(1000));
		given(event.getPayeeDetails().getAddress().getState()).willReturn("CA");
		given(event.getPayeeDetails().getTaxId()).willReturn("123456789");
		CardHolderDto cardHolder = CardHolderDto.builder().build();
		LocalDate expiryDate = LocalDate.of(2025, 12, 31);
		SpendRules spendRules = new SpendRules();
		spendRules.setRuleCode(new ArrayList<>());
		SubAccount subAccount = new SubAccount();
		subAccount.setFundingMethod("FUND");
		Bin bin = new Bin();
		bin.setBin(123456L);
		given(this.consentDecisionService.determineAutoActivateCard(event)).willReturn(true);
		given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACC", "SUB",
				"ORG"))
			.willReturn(Optional.of(subAccount));
		given(this.binRepository.findByFundingType("FUND")).willReturn(Optional.of(bin));
		// when
		CreateCardRequest request = this.service.buildCardCreationRequest(event, cardHolder, expiryDate, spendRules);
		// then
		assertThat(request).isNotNull();
		assertThat(request.getBin()).isEqualTo(123456L);
		assertThat(request.getAmount()).isEqualTo(100000L);

	}

	@Test
	@DisplayName("should throw exception when subAccount not found")
	void shouldThrowExceptionWhenSubAccountNotFound() {
		// given
		PaymentRequestDto event = new PaymentRequestDto();
		event.setAccountShortCode("ACC");
		event.setSubAccountShortCode("SUB");
		event.setOrgShortCode("ORG");
		CardHolderDto cardHolder = CardHolderDto.builder().build();
		LocalDate expiryDate = LocalDate.of(2025, 12, 31);
		SpendRules spendRules = new SpendRules();
		given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACC", "SUB",
				"ORG"))
			.willReturn(Optional.empty());
		// when, then
		assertThatThrownBy(() -> this.service.buildCardCreationRequest(event, cardHolder, expiryDate, spendRules))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessage("SubAccount not found for short code: SUB");
	}

	@Test
	@DisplayName("should throw exception when bin not found")
	void shouldThrowExceptionWhenBinNotFound() {
		// given
		PaymentRequestDto event = new PaymentRequestDto();
		event.setAccountShortCode("ACC");
		event.setSubAccountShortCode("SUB");
		event.setOrgShortCode("ORG");
		PaymentDetailsDto paymentDetails = new PaymentDetailsDto();
		paymentDetails.setAmount(BigDecimal.valueOf(1000));
		event.setPaymentDetails(paymentDetails);
		CardHolderDto cardHolder = CardHolderDto.builder().build();
		LocalDate expiryDate = LocalDate.of(2025, 12, 31);
		SpendRules spendRules = new SpendRules();
		SubAccount subAccount = new SubAccount();
		subAccount.setFundingMethod("FUND");
		given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACC", "SUB",
				"ORG"))
			.willReturn(Optional.of(subAccount));
		given(this.binRepository.findByFundingType("FUND")).willReturn(Optional.empty());
		// when, then
		assertThatThrownBy(() -> this.service.buildCardCreationRequest(event, cardHolder, expiryDate, spendRules))
			.isInstanceOf(CardProcessingError.class)
			.hasMessage("Bin not found for funding type: FUND");
	}

	@Test
	@DisplayName("should throw clear exception when multiple bins exist for funding type")
	void shouldThrowClearExceptionWhenMultipleBinsExistForFundingType() {
		// given
		PaymentRequestDto event = new PaymentRequestDto();
		event.setAccountShortCode("ACC");
		event.setSubAccountShortCode("SUB");
		event.setOrgShortCode("ORG");
		PaymentDetailsDto paymentDetails = new PaymentDetailsDto();
		paymentDetails.setAmount(BigDecimal.valueOf(1000));
		paymentDetails.setIssuerPaymentId("issuer-123");
		event.setPaymentDetails(paymentDetails);
		CardHolderDto cardHolder = CardHolderDto.builder().build();
		LocalDate expiryDate = LocalDate.of(2025, 12, 31);
		SpendRules spendRules = new SpendRules();
		SubAccount subAccount = new SubAccount();
		subAccount.setFundingMethod("FUND");
		given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACC", "SUB",
				"ORG"))
			.willReturn(Optional.of(subAccount));
		given(this.binRepository.findByFundingType("FUND")).willThrow(new IncorrectResultSizeDataAccessException(1, 2));

		// when, then
		assertThatThrownBy(() -> this.service.buildCardCreationRequest(event, cardHolder, expiryDate, spendRules))
			.isInstanceOf(CardProcessingError.class)
			.hasMessage("Multiple BIN configurations found for funding type: FUND");
	}

	@Test
	@DisplayName("should find spend rules at subAccount level")
	void shouldFindSpendRulesAtSubAccountLevel() {
		// given
		SpendRules rules = new SpendRules();
		given(this.spendRulesConfigRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode("ORG", "ACC",
				"SUB"))
			.willReturn(Optional.of(rules));
		// when
		Optional<SpendRules> result = this.service.findExistingSpendRules("ORG", "ACC", "SUB");
		// then
		assertThat(result).isPresent();
	}

	@Test
	@DisplayName("should fallback to account level if subAccount not found")
	void shouldFallbackToAccountLevelIfSubAccountNotFound() {
		// given
		SpendRules rules = new SpendRules();
		given(this.spendRulesConfigRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode("ORG", "ACC",
				"SUB"))
			.willReturn(Optional.empty());
		given(this.spendRulesConfigRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCodeIsNull("ORG",
				"ACC"))
			.willReturn(Optional.of(rules));
		// when
		Optional<SpendRules> result = this.service.findExistingSpendRules("ORG", "ACC", "SUB");
		// then
		assertThat(result).isPresent();
	}

	@Test
	@DisplayName("should fallback to org level if account not found")
	void shouldFallbackToOrgLevelIfAccountNotFound() {
		// given
		SpendRules rules = new SpendRules();
		given(this.spendRulesConfigRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode("ORG", "ACC",
				"SUB"))
			.willReturn(Optional.empty());
		given(this.spendRulesConfigRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCodeIsNull("ORG",
				"ACC"))
			.willReturn(Optional.empty());
		given(this.spendRulesConfigRepository
			.findByOrgShortCodeAndAccountShortCodeIsNullAndSubAccountShortCodeIsNull("ORG"))
			.willReturn(Optional.of(rules));
		// when
		Optional<SpendRules> result = this.service.findExistingSpendRules("ORG", "ACC", "SUB");
		// then
		assertThat(result).isPresent();
	}

	@Test
	@DisplayName("should return empty if no rules found")
	void shouldReturnEmptyIfNoRulesFound() {
		// given
		given(this.spendRulesConfigRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode("ORG", "ACC",
				"SUB"))
			.willReturn(Optional.empty());
		given(this.spendRulesConfigRepository.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCodeIsNull("ORG",
				"ACC"))
			.willReturn(Optional.empty());
		given(this.spendRulesConfigRepository
			.findByOrgShortCodeAndAccountShortCodeIsNullAndSubAccountShortCodeIsNull("ORG"))
			.willReturn(Optional.empty());
		// when
		Optional<SpendRules> result = this.service.findExistingSpendRules("ORG", "ACC", "SUB");
		// then
		assertThat(result).isEmpty();
	}

	@Test
	@DisplayName("should build card holder details")
	void shouldBuildCardHolderDetails() {
		// given
		CardHolder cardHolder = new CardHolder();
		cardHolder.setEmail("test@example.com");
		cardHolder.setFirstName("John");
		cardHolder.setLastName("Doe");
		cardHolder.setPhoneNumber("1234567890");
		BillingAddress billingAddress = new BillingAddress();
		billingAddress.setLine1("123 Main St");
		cardHolder.setBillingAddress(billingAddress);
		// when
		CardHolderDto dto = this.service.buildCardHolderDetails(cardHolder);
		// then
		assertThat(dto).isNotNull();
		assertThat(dto.getEmail()).isEqualTo("test@example.com");
		assertThat(dto.getBillingAddress().getLine1()).isEqualTo("123 Main St");
	}

	@Test
	@DisplayName("constructCardExpiry should handle null, empty, valid, and invalid expiryDays")
	void constructCardExpiryShouldHandleVariousInputs() {
		// given
		Integer validDays = 10;
		Integer emptyDays = null;
		long defaultDays = 30L;
		ReflectionTestUtils.setField(this.service, "defaultExpiryDays", defaultDays);
		// when
		java.time.LocalDate resultValid = this.service.constructCardExpiry(validDays);
		java.time.LocalDate resultEmpty = this.service.constructCardExpiry(emptyDays);

		// then
		assertThat(resultValid).isEqualTo(java.time.LocalDate.now().plusDays(10));
		assertThat(resultEmpty).isEqualTo(java.time.LocalDate.now().plusDays(defaultDays));

	}

	@Test
	@DisplayName("saveCardCreationRequest should save and return correct CardCreation")
	void saveCardCreationRequestShouldSaveAndReturnCardCreation() {
		// given
		PaymentRequestDto event = new PaymentRequestDto();
		event.setPaymentDetails(new PaymentDetailsDto());
		event.getPaymentDetails().setId("paymentId");
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(123456L)
			.amount(100000L)
			.expiry(new Expiry("12", "25"))
			.cardHolder(CardHolderDto.builder().build())
			.spendRules(new ArrayList<>())
			.build();
		CardHolder cardholder = new CardHolder();
		LocalDate expiryDate = LocalDate.of(2025, 12, 31);
		SpendRules spendRules = new SpendRules();
		CardCreation expected = new CardCreation();
		given(this.cardCreationRepository.save(any())).willReturn(expected);
		// when
		CardCreation result = this.service.saveCardCreationRequest(event, request, cardholder, expiryDate, spendRules);
		// then
		assertThat(result).isSameAs(expected);
	}

	// -----------------------------------------------------------------------
	// buildCardCreationRequest – validation tests
	// -----------------------------------------------------------------------

	@Test
	@DisplayName("buildCardCreationRequest should throw CardProcessingError when amount is 0")
	void buildCardCreationRequestShouldThrowErrorWhenAmountIsZero() {
		// given
		PaymentRequestDto event = mock(PaymentRequestDto.class, RETURNS_DEEP_STUBS);
		given(event.getAccountShortCode()).willReturn("ACC");
		given(event.getSubAccountShortCode()).willReturn("SUB");
		given(event.getOrgShortCode()).willReturn("ORG");
		given(event.getPaymentDetails().getAmount()).willReturn(BigDecimal.ZERO);
		given(event.getPaymentDetails().getId()).willReturn("PAY123");

		SubAccount subAccount = createSubAccount("creditCard");
		Bin bin = createBin(541212L);
		given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACC", "SUB",
				"ORG"))
			.willReturn(Optional.of(subAccount));
		given(this.binRepository.findByFundingType("creditCard")).willReturn(Optional.of(bin));
		given(this.consentDecisionService.determineAutoActivateCard(event)).willReturn(true);
		given(this.validator.validate(any()))
			.willReturn(java.util.Set.of(org.mockito.Mockito.mock(jakarta.validation.ConstraintViolation.class)));

		CardHolderDto cardHolder = CardHolderDto.builder().firstName("John").lastName("Doe").build();
		LocalDate expiryDate = LocalDate.of(2025, 12, 31);

		// when / then
		// Validation error is thrown here; P3 logging happens at the caller
		// (PaymentRequestServiceImpl.processPaymentRequestEvents)
		assertThatThrownBy(() -> this.service.buildCardCreationRequest(event, cardHolder, expiryDate, new SpendRules()))
			.isInstanceOf(CardProcessingError.class)
			.hasMessageContaining("Card creation request validation failed")
			.hasMessageContaining("PAY123");
	}

	@Test
	@DisplayName("buildCardCreationRequest should succeed with valid amount")
	void buildCardCreationRequestShouldSucceedWithValidAmount() {
		// given
		PaymentRequestDto event = createValidBuildRequestEvent();
		SubAccount subAccount = createSubAccount("creditCard");
		Bin bin = createBin(541212L);
		given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACC", "SUB",
				"ORG"))
			.willReturn(Optional.of(subAccount));
		given(this.binRepository.findByFundingType("creditCard")).willReturn(Optional.of(bin));
		given(this.consentDecisionService.determineAutoActivateCard(event)).willReturn(true);
		given(this.validator.validate(any())).willReturn(java.util.Collections.emptySet());

		CardHolderDto cardHolder = CardHolderDto.builder().firstName("John").lastName("Doe").build();
		LocalDate expiryDate = LocalDate.of(2025, 12, 31);
		SpendRules spendRules = new SpendRules();
		spendRules.setRuleCode(new ArrayList<>());

		// when
		CreateCardRequest result = this.service.buildCardCreationRequest(event, cardHolder, expiryDate, spendRules);

		// then
		assertThat(result).isNotNull();
		assertThat(result.getAmount()).isEqualTo(100000L); // 1000 * 100 = 100000 cents
	}

	private PaymentRequestDto createValidBuildRequestEvent() {
		PaymentRequestDto event = mock(PaymentRequestDto.class, RETURNS_DEEP_STUBS);
		given(event.getAccountShortCode()).willReturn("ACC");
		given(event.getSubAccountShortCode()).willReturn("SUB");
		given(event.getOrgShortCode()).willReturn("ORG");
		given(event.getPaymentDetails().getAmount()).willReturn(BigDecimal.valueOf(1000));
		return event;
	}

	private SubAccount createSubAccount(String fundingMethod) {
		SubAccount subAccount = new SubAccount();
		subAccount.setFundingMethod(fundingMethod);
		return subAccount;
	}

	private Bin createBin(long binValue) {
		Bin bin = new Bin();
		bin.setBin(binValue);
		return bin;
	}

}
