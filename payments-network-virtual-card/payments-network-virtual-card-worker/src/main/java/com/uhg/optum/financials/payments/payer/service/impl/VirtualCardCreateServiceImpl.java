package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.BillingAddress;
import com.uhg.optum.financials.payments.payer.dto.CardHolderDto;
import com.uhg.optum.financials.payments.payer.dto.CreateCardRequest;
import com.uhg.optum.financials.payments.payer.dto.Expiry;
import com.uhg.optum.financials.payments.payer.dto.ResponseObject;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.exception.CardProcessingError;
import com.uhg.optum.financials.payments.payer.exception.ZetaRestClientException;
import com.uhg.optum.financials.payments.payer.model.Bin;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.CardHolder;
import com.uhg.optum.financials.payments.payer.model.SpendRules;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.repository.BinRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.SpendRulesConfigRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.ConsentDecisionService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardCreateService;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.AmountConverter;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.CardOperationLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.WebhookAlertUtil;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.dao.IncorrectResultSizeDataAccessException;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

import jakarta.validation.ConstraintViolation;
import jakarta.validation.Validator;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD_CREATION_STATUS_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD_CREATION_REQUEST_FAILED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.CREATE;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_ACCEPTED;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_INITIATED;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_REJECTED;

@Slf4j
@Service
public class VirtualCardCreateServiceImpl implements VirtualCardCreateService {

	@Autowired
	LogUtility logUtility;

	@Autowired
	private ZetaApiService zetaApiService;

	@Autowired
	private CardCreationRepository cardCreationRepository;

	@Autowired
	private BinRepository binRepository;

	@Autowired
	private SubAccountRepository subAccountRepository;

	@Autowired
	private SpendRulesConfigRepository spendRulesConfigRepository;

	@Autowired
	private ConsentDecisionService consentDecisionService;

	@Value("${zeta.maxRetryCount:3}")
	private int maxRetryCount;

	@Value("${zeta.default-expiry-days:45}")
	private long defaultExpiryDays;

	@Autowired
	private AuditLogger auditLogger;

	@Autowired
	private CardOperationLogger cardOperationLogger;

	@Autowired
	WebhookAlertUtil webhookAlertUtil;

	@Autowired
	private Validator validator;

	@Override
	public CreateCardRequest buildCardCreationRequest(PaymentRequestDto event, CardHolderDto cardHolder,
			LocalDate expiryDate, SpendRules spendRules) {
		boolean autoActivateCard = this.consentDecisionService.determineAutoActivateCard(event);
		SubAccount subAccount = this.subAccountRepository
			.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(event.getAccountShortCode(),
					event.getSubAccountShortCode(), event.getOrgShortCode())
			.orElseThrow(() -> new IllegalArgumentException(
					"SubAccount not found for short code: " + event.getSubAccountShortCode()));
		Bin bin;
		try {
			bin = this.binRepository.findByFundingType(subAccount.getFundingMethod()).orElseThrow(() -> {
				String errorMessage = "Bin not found for funding type: " + subAccount.getFundingMethod();
				this.logUtility.logMessageWithPriority("Payment request failed for paymentId: "
						+ ((event.getPaymentDetails() != null) ? event.getPaymentDetails().getIssuerPaymentId() : "N/A")
						+ ERROR_MESSAGE + errorMessage, ERROR, log, P3);
				return new CardProcessingError(errorMessage);
			});
		}
		catch (IncorrectResultSizeDataAccessException ex) {
			String errorMessage = "Multiple BIN configurations found for funding type: "
					+ subAccount.getFundingMethod();
			this.logUtility.logMessageWithPriority(
					"Payment request failed for paymentId: " + ((event.getPaymentDetails() != null)
							? event.getPaymentDetails().getIssuerPaymentId() : "N/A") + ERROR_MESSAGE + errorMessage,
					ERROR, log, P3);
			throw new CardProcessingError(errorMessage, ex);
		}
		String month = String.format("%02d", expiryDate.getMonthValue());
		String year = String.format("%02d", expiryDate.getYear() % 100);
		CreateCardRequest createCardRequest = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(bin.getBin())
			.amount(AmountConverter.toCents(event.getPaymentDetails().getAmount()))
			.autoActivateCard(autoActivateCard)
			.singleSpend(false) // /** TODO - hardcoded it right now must fetch from
								// subaccount table **/
			.expiry(new Expiry(month, year))
			.cardHolder(cardHolder)
			.spendRules((spendRules != null && spendRules.getRuleCode() != null) ? spendRules.getRuleCode()
					: new ArrayList<>())
			.build();

		// Validate the CreateCardRequest to ensure all constraints are met
		validateCreateCardRequest(createCardRequest, event);

		return createCardRequest;
	}

	@Override
	public Optional<SpendRules> findExistingSpendRules(String orgShortCd, String accountShortCd,
			String subAccountShortCd) {
		Optional<SpendRules> existingSpendRules;
		existingSpendRules = this.spendRulesConfigRepository
			.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode(orgShortCd, accountShortCd, subAccountShortCd);
		if (existingSpendRules.isEmpty()) {
			existingSpendRules = this.spendRulesConfigRepository
				.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCodeIsNull(orgShortCd, accountShortCd);
			if (existingSpendRules.isEmpty()) {
				existingSpendRules = this.spendRulesConfigRepository
					.findByOrgShortCodeAndAccountShortCodeIsNullAndSubAccountShortCodeIsNull(orgShortCd);
			}
		}
		return existingSpendRules;
	}

	@Override
	public CardHolderDto buildCardHolderDetails(CardHolder cardHolder) {
		return CardHolderDto.builder()
			.email(cardHolder.getEmail())
			.firstName(cardHolder.getFirstName())
			.lastName(cardHolder.getLastName())
			.phoneNumber(nullIfBlank(cardHolder.getPhoneNumber()))
			.billingAddress(buildBillingAddress(cardHolder.getBillingAddress()))
			.build();
	}

	private BillingAddress buildBillingAddress(
			com.uhg.optum.financials.payments.payer.model.BillingAddress billingAddress) {
		return BillingAddress.builder()
			.line1(billingAddress.getLine1())
			.city(billingAddress.getCity())
			.country(billingAddress.getCountry())
			.postalCode(billingAddress.getPostalCode())
			.build();
	}

	@Override
	public LocalDate constructCardExpiry(Integer expiryDays) {
		long daysToAdd;
		if (expiryDays == null) {
			daysToAdd = this.defaultExpiryDays;
		}
		else {
			daysToAdd = expiryDays;
		}
		return LocalDate.now().plusDays(daysToAdd);
	}

	@Override
	public CardCreation saveCardCreationRequest(PaymentRequestDto event, CreateCardRequest request,
			CardHolder cardholder, LocalDate expiryDate, SpendRules spendRules) {
		var cardCreation = new CardCreation();
		cardCreation.setId(request.getRequestId());
		cardCreation.setPaymentId(event.getPaymentDetails().getId());
		cardCreation.setBin(request.getBin());
		cardCreation.setAmount(request.getAmount());
		cardCreation.setExpiryMonth(request.getExpiry().getMonth());
		cardCreation.setExpiryYear(request.getExpiry().getYear());
		cardCreation.setCardHolder(cardholder);
		cardCreation.setExpiryDate(expiryDate);
		cardCreation.setSpendRule(spendRules);
		cardCreation.setIsSingleSpend(request.getSingleSpend());
		cardCreation.setStatus(REQUEST_INITIATED.name());
		cardCreation.setCreatedAt(LocalDateTime.now());
		cardCreation.setCreatedBy(VIRTUAL_CARD);
		return this.cardCreationRepository.save(cardCreation);
	}

	@Override
	public void sendCardCreationRequestToZeta(CreateCardRequest createCardRequest, CardCreation cardCreation) {
		this.cardOperationLogger.logCardProcessStarted(CardEventType.CREATE, cardCreation.getPaymentId(),
				cardCreation.getId().toString(), createCardRequest, cardCreation.getStatus(), log);
		ResponseEntity<ResponseObject> response = null;
		try {
			this.auditLogger.logCreateInitiated(cardCreation.getId(), cardCreation.getPaymentId());
			response = (ResponseEntity<ResponseObject>) this.zetaApiService.postVirtualCard(createCardRequest);

			if (response.getStatusCode().is2xxSuccessful()) {
				cardCreation.setStatus(REQUEST_ACCEPTED.name());
				cardCreation.setUpdatedBy(VIRTUAL_CARD);
				cardCreation.setUpdatedAt(LocalDateTime.now());
				this.logUtility.logMessage("Card creation response received with status: " + response + " | "
						+ CARD_CREATION_STATUS_MESSAGE + cardCreation.getStatus(), INFO, log);
				this.cardCreationRepository.save(cardCreation);
				this.auditLogger.logCreateAccepted(cardCreation.getId(), cardCreation.getPaymentId());
				// If a webhook response is not received within the defined cut-off time,
				// the system should trigger an alert.
				this.webhookAlertUtil.scheduleTimeout(cardCreation.getId(), CREATE);

			}
			else if (response.getStatusCode().is4xxClientError() || response.getStatusCode().is5xxServerError()) {
				cardCreation.setStatus(REQUEST_REJECTED.name());
				cardCreation.setRetryCount(response.getBody().getRetryCount());
				cardCreation.setErrorCode(VIRTUAL_CARD_CREATION_REQUEST_FAILED);
				String errorMessage = response.getStatusCode()
						+ ((response.getBody() != null && response.getBody().getMessage() != null)
								? " - " + response.getBody().getMessage() : "");
				cardCreation.setErrorMessage(errorMessage);
				cardCreation.setUpdatedBy(VIRTUAL_CARD);
				cardCreation.setUpdatedAt(LocalDateTime.now());
				this.logUtility.logMessage("Card creation response received with status: " + response + " | "
						+ CARD_CREATION_STATUS_MESSAGE + cardCreation.getStatus(), INFO, log);
				this.cardCreationRepository.save(cardCreation);
				this.auditLogger.logCreateRejected(cardCreation.getId(), cardCreation.getPaymentId());
				this.logUtility.logMessageWithPriority("Payment request failed for paymentId: "
						+ cardCreation.getPaymentId() + ERROR_MESSAGE + errorMessage, ERROR, log, P3);
				throw new ZetaRestClientException("Payment request failed for paymentId: " + cardCreation.getPaymentId()
						+ ERROR_MESSAGE + errorMessage, response.getStatusCode().value());
			}
			else {
				this.cardOperationLogger.logCardProcessError(CardEventType.CREATE, cardCreation.getPaymentId(),
						cardCreation.getId().toString(), response, log);
				this.auditLogger.logError(cardCreation.getId(), cardCreation.getPaymentId(), null, CardEventType.CREATE,
						this.cardOperationLogger.extractResponseMessage(response));
				this.logUtility.logMessageWithPriority(
						"Payment request failed for paymentId: " + cardCreation.getPaymentId() + ERROR_MESSAGE
								+ this.cardOperationLogger.extractResponseMessage(response),
						ERROR, log, P3);
				throw new CardProcessingError("Unexpected response status from virtual card creation service");
			}

		}
		catch (CardProcessingError cpe) {
			this.logUtility.logMessage("Unexpected exception occurred: " + cpe.getMessage(), INFO, log);
			throw cpe;
		}
		catch (ZetaRestClientException ex) {
			throw ex;
		}
		catch (Exception ex) {
			this.cardOperationLogger.logCardProcessException(CardEventType.CREATE, cardCreation.getPaymentId(),
					cardCreation.getId().toString(), ex, log);
			this.auditLogger.logException(cardCreation.getId(), null, cardCreation.getPaymentId(), null,
					CardEventType.CREATE, ex);
			this.logUtility
				.logMessageWithPriority("Payment request failed for paymentId: " + cardCreation.getPaymentId()
						+ ERROR_MESSAGE + this.cardOperationLogger.extractResponseMessage(response), ERROR, log, P3);
			throw new RuntimeException("Unexpected response status from virtual card creation service: " + ex);
		}
		this.cardOperationLogger.logCardProcessCompleted(CardEventType.CREATE, cardCreation.getPaymentId(),
				cardCreation.getId().toString(), this.cardOperationLogger.extractResponseMessage(response),
				cardCreation.getStatus(), log);
	}

	/**
	 * Validates the CreateCardRequest to ensure all JSR-303 validation constraints are
	 * met before sending to Zeta API. This method performs validation only; error logging
	 * and payment status updates are handled at the caller level
	 * (PaymentRequestServiceImpl).
	 * @param createCardRequest the request to validate
	 * @param event the payment request event for error message construction
	 * @throws CardProcessingError if validation fails
	 */
	private void validateCreateCardRequest(CreateCardRequest createCardRequest, PaymentRequestDto event) {
		Set<ConstraintViolation<CreateCardRequest>> violations = this.validator.validate(createCardRequest);
		if (!violations.isEmpty()) {
			String paymentId = (event.getPaymentDetails() != null) ? event.getPaymentDetails().getId() : "UNKNOWN";
			String violationMessages = violations.stream()
				.map((v) -> v.getPropertyPath() + ": " + v.getMessage())
				.reduce("", (a, b) -> a.isEmpty() ? b : a + "; " + b);
			String errorMsg = "Card creation request validation failed for paymentId: " + paymentId + " - "
					+ violationMessages;
			throw new CardProcessingError(errorMsg);
		}
	}

	private String nullIfBlank(String s) {
		if (s == null) {
			return null;
		}
		s = s.trim();
		return s.isEmpty() ? null : s;
	}

}
