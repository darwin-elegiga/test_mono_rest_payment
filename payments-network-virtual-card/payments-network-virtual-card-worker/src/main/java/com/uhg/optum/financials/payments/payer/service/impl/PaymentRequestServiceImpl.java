package com.uhg.optum.financials.payments.payer.service.impl;

import com.google.gson.Gson;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.dto.CardHolderDto;
import com.uhg.optum.financials.payments.payer.dto.CreateCardRequest;
import com.uhg.optum.financials.payments.payer.dto.RulesResponse;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountBankAccountDetails;
import com.uhg.optum.financials.payments.payer.exception.OrganizationRegisteredException;
import com.uhg.optum.financials.payments.payer.exception.ZetaRestClientException;
import com.uhg.optum.financials.payments.payer.exception.NetworkPaymentIdGenerationException;
import com.uhg.optum.financials.payments.payer.exception.SegmentGenerationException;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.CardProcessingError;
import com.uhg.optum.financials.payments.payer.enums.CardOperationStatus;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.CardHolder;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.PaymentRequestHistory;
import com.uhg.optum.financials.payments.payer.model.SpendRules;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardholderRepository;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestHistoryRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.ReprocessLockRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.PaymentRequestService;
import com.uhg.optum.financials.payments.payer.service.SegmentCreationService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardCreateService;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.PaymentRequestEntityUtil;
import com.uhg.optum.financials.payments.payer.util.PaymentTypeCategoryUtil;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import jakarta.persistence.EntityNotFoundException;
import jakarta.validation.ConstraintViolation;
import jakarta.validation.Validator;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;
import org.springframework.validation.annotation.Validated;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.enums.Status.PE;
import static com.uhg.optum.financials.payments.payer.enums.Status.PI;
import static com.uhg.optum.financials.shared.client.constant.PaymentType.REMITTANCE_ONLY;

@Slf4j
@Service
@Validated
public class PaymentRequestServiceImpl implements PaymentRequestService {

	private final VirtualCardCreateService virtualCardCreateService;

	private final LogUtility logUtility;

	private final PaymentRequestRepository paymentRequestRepository;

	private final CardholderRepository cardholderRepository;

	private final PaymentRequestHistoryRepository paymentRequestHistoryRepository;

	private final OrganizationRepository organizationRepository;

	private final SegmentCreationService segmentCreationService;

	private final SubAccountRepository subAccountRepository;

	private final PaymentDetailsRepository paymentDetailsRepository;

	private final ZetaApiService zetaApiService;

	private final Validator validator;

	private final CardCreationRepository cardCreationRepository;

	private final ReprocessLockRepository reprocessLockRepository;

	private static final Gson JSON = new Gson();

	public static final String ALERT_MESSAGE = "Raising P3 alert: ";

	public PaymentRequestServiceImpl(VirtualCardCreateService virtualCardCreateService, LogUtility logUtility,
			PaymentRequestRepository paymentRequestRepository, CardholderRepository cardholderRepository,
			PaymentRequestHistoryRepository paymentRequestHistoryRepository,
			OrganizationRepository organizationRepository, SegmentCreationService segmentCreationService,
			SubAccountRepository subAccountRepository, PaymentDetailsRepository paymentDetailsRepository,
			ZetaApiService zetaApiService, Validator validator, CardCreationRepository cardCreationRepository,
			ReprocessLockRepository reprocessLockRepository) {
		this.virtualCardCreateService = virtualCardCreateService;
		this.logUtility = logUtility;
		this.paymentRequestRepository = paymentRequestRepository;
		this.cardholderRepository = cardholderRepository;
		this.cardCreationRepository = cardCreationRepository;
		this.paymentRequestHistoryRepository = paymentRequestHistoryRepository;
		this.organizationRepository = organizationRepository;
		this.segmentCreationService = segmentCreationService;
		this.subAccountRepository = subAccountRepository;
		this.paymentDetailsRepository = paymentDetailsRepository;
		this.zetaApiService = zetaApiService;
		this.validator = validator;
		this.reprocessLockRepository = reprocessLockRepository;
	}

	@Override
	public void processPaymentRequestEvents(PaymentRequestDto event) {
		this.logUtility.logMessage(
				String.format("[correlationId=%s] [paymentId=%s] Worker Module - Start processing payment request",
						event.getCorrelationId(), event.getPaymentDetails().getId()),
				AppConstants.INFO, log);
		try {
			processPaymentRequestEventsInternal(event);
		}
		finally {
			// Held for the entire duration of processing, not just until it starts —
			// releasing any earlier would let a concurrent reprocess trigger race this
			// same submission attempt before its outcome (REQUEST_ACCEPTED, or a
			// terminal failure) is actually visible. Harmless no-op for a normal
			// (non-reprocessed) submission, which never acquired this lock.
			this.reprocessLockRepository.releaseLock("payment:" + event.getPaymentDetails().getId());
		}
	}

	private void processPaymentRequestEventsInternal(PaymentRequestDto event) {
		try {
			savePaymentRequest(event);
		}
		catch (SegmentGenerationException ex) {
			return;
		}
		try {
			LocalDate expiryDate;
			Optional<SubAccount> subAccount = this.subAccountRepository
				.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(event.getAccountShortCode(),
						event.getSubAccountShortCode(), event.getOrgShortCode());

			if (subAccount.isPresent()) {
				this.logUtility.logMessage("Fetching Sub account expiry days details", INFO, log);
				expiryDate = this.virtualCardCreateService.constructCardExpiry(subAccount.get().getExpiryDays());
			}
			else {
				throw new OrganizationRegisteredException(
						"Sub account detail not found for this sub account short code: "
								+ event.getSubAccountShortCode());
			}

			CardHolder existingCardHolder = this.cardholderRepository.findCardholder();
			CardHolderDto cardHolder = this.virtualCardCreateService.buildCardHolderDetails(existingCardHolder);
			try {
				validateCardHolderDto(cardHolder, event.getPaymentDetails().getId());
			}
			catch (BadRequestException ex) {
				updatePaymentRequestWithError(event.getPaymentDetails().getId(),
						"CardHolder validation failed: " + ex.getMessage());

				this.logUtility.logMessageWithPriority(
						ALERT_MESSAGE + "Payment request updated with PE status and error message for paymentId: "
								+ event.getPaymentDetails().getId() + " - " + ex.getMessage(),
						ERROR, log, P3);
				return;
			}
			SpendRules existingSpendRules = this.virtualCardCreateService
				.findExistingSpendRules(event.getOrgShortCode(), event.getAccountShortCode(),
						event.getSubAccountShortCode())
				.orElse(null);
			// Fetch global rules and include them in the card creation request.
			SpendRules spendRules = getGlobalRules(existingSpendRules);
			try {
				validateSpendRules(spendRules, event.getPaymentDetails().getId());
			}
			catch (BadRequestException ex) {
				updatePaymentRequestWithError(event.getPaymentDetails().getId(),
						"SpendRules validation failed: " + ex.getMessage());

				this.logUtility.logMessageWithPriority(
						ALERT_MESSAGE + "Payment request updated with PE status and error message for paymentId: "
								+ event.getPaymentDetails().getId() + " - " + ex.getMessage(),
						ERROR, log, P3);
				return;
			}
			Optional<CardCreation> latestCardCreation = this.cardCreationRepository
				.findTopByPaymentIdOrderByCreatedAtDesc(event.getPaymentDetails().getId());
			boolean alreadyHandledByZeta = latestCardCreation.isPresent()
					&& (CardOperationStatus.REQUEST_ACCEPTED.name().equals(latestCardCreation.get().getStatus())
							|| CardOperationStatus.CARD_ISSUED.name().equals(latestCardCreation.get().getStatus()));

			if (alreadyHandledByZeta) {
				// A prior attempt already got this far — resubmitting would call Zeta a
				// second time for a card that is already in flight or already issued.
				this.logUtility.logMessage(
						"Card creation already " + latestCardCreation.get().getStatus() + " for paymentId: "
								+ event.getPaymentDetails().getId() + " — skipping duplicate Zeta request",
						AppConstants.INFO, log);
			}
			else {
				CreateCardRequest createCardRequest = this.virtualCardCreateService.buildCardCreationRequest(event,
						cardHolder, expiryDate, spendRules);
				if (existingSpendRules == null) {
					spendRules = null;
				}
				var cardCreation = this.virtualCardCreateService.saveCardCreationRequest(event, createCardRequest,
						existingCardHolder, expiryDate, spendRules);
				this.virtualCardCreateService.sendCardCreationRequestToZeta(createCardRequest, cardCreation);
			}
			this.logUtility.logMessage(String.format(
					"[correlationId=%s] [paymentId=%s] Worker Module - Finished processing payment request",
					event.getCorrelationId(), event.getPaymentDetails().getId()), AppConstants.INFO, log);
		}
		catch (ZetaRestClientException | CardProcessingError ex) {
			updatePaymentRequestWithError(event.getPaymentDetails().getId(), ex.getMessage());

			this.logUtility.logMessageWithPriority(
					ALERT_MESSAGE + "Payment request updated with PE status and error message for paymentId: "
							+ event.getPaymentDetails().getId() + " - " + ex.getMessage(),
					ERROR, log, P3);
		}
		catch (Exception ex) {
			String errorMsg = "Error processing payment request for paymentId: " + event.getPaymentDetails().getId()
					+ " " + AppConstants.ERROR_MESSAGE + " " + ex.getMessage();
			this.logUtility.logMessage(errorMsg, ERROR, log);
			// Update payment_request table with PE status and error message
			updatePaymentRequestWithError(event.getPaymentDetails().getId(), errorMsg);
			throw ex;
		}
	}

	private String appendErrorMessage(String existingErrorMessage, String nextErrorMessage) {
		if (!StringUtils.hasText(nextErrorMessage)) {
			return existingErrorMessage;
		}
		if (!StringUtils.hasText(existingErrorMessage)) {
			return nextErrorMessage;
		}
		return existingErrorMessage + System.lineSeparator() + nextErrorMessage;
	}

	private void savePaymentRequest(PaymentRequestDto requestDto) {
		String submitterId = validateAndGetSubmitterId(requestDto);
		SubAccountBankAccountDetails subAccountDetails = fetchSubAccountDetails(requestDto);
		if (this.paymentRequestRepository.existsByPaymentDetailsId(requestDto.getPaymentDetails().getId())) {
			handleExistingPaymentRequest(requestDto, subAccountDetails);
		}
		else {
			this.logUtility.logMessage("Saving payment request to DB", INFO, log);

			PaymentRequest paymentRequest = PaymentRequestEntityUtil.toPaymentRequestEntity(requestDto);
			String networkPaymentId = createNetworkPaymentId(requestDto);
			paymentRequest.getPaymentDetails().setNetworkPaymentId(networkPaymentId);
			paymentRequest.setStatus(PI.name());
			paymentRequest.setSubmitterId(submitterId);
			paymentRequest.setCreatedById(VIRTUAL_CARD);
			paymentRequest.setUpdatedById(VIRTUAL_CARD);
			paymentRequest.setPayr835Id(subAccountDetails.getPayer835Id());
			this.createStSeSegmentAndSet(requestDto, subAccountDetails, paymentRequest);
			this.paymentRequestRepository.save(paymentRequest);
			this.logUtility.logMessage("Payment request saved successfully", INFO, log);
			savePaymentRequestHistory(paymentRequest);
		}
	}

	private String validateAndGetSubmitterId(PaymentRequestDto requestDto) {
		if (requestDto == null || requestDto.getPayeeDetails() == null
				|| !StringUtils.hasText(requestDto.getPayeeDetails().getReceiverId())) {
			throw new BadRequestException("receiverId is required");
		}
		return requestDto.getPayeeDetails().getReceiverId().trim();
	}

	private String createNetworkPaymentId(PaymentRequestDto requestDto) {
		String paymentId = (requestDto.getPaymentDetails() != null) ? requestDto.getPaymentDetails().getId()
				: "UNKNOWN";
		String paymentType = (requestDto.getPaymentDetails() != null)
				? String.valueOf(requestDto.getPaymentDetails().getType()) : null;
		String payeeType = PaymentTypeCategoryUtil.toC2bOrB2b(paymentType);
		try {
			Long sequenceValue = this.paymentDetailsRepository.getNextNetworkPaymentIdSequence();
			if (sequenceValue == null) {
				String errorMsg = "Network Payment ID sequence returned null for payment: " + paymentId;
				this.logUtility.logMessageWithPriority(errorMsg, ERROR, log, "P2");
				throw new NetworkPaymentIdGenerationException(errorMsg, "SEQUENCE_GENERATION_FAILED", paymentId);
			}
			return PaymentRequestEntityUtil.generateNetworkPaymentId(payeeType, sequenceValue);
		}
		catch (NetworkPaymentIdGenerationException ex) {
			throw ex;
		}
		catch (Exception ex) {
			String errorMsg = "Failed to generate Network Payment ID for payment: " + paymentId + " - "
					+ ex.getMessage();
			this.logUtility.logMessageWithPriority(errorMsg, ERROR, log, "P2");
			throw new NetworkPaymentIdGenerationException(errorMsg, "SEQUENCE_QUERY_FAILED", paymentId, ex);
		}
	}

	private void handleExistingPaymentRequest(PaymentRequestDto requestDto,
			SubAccountBankAccountDetails subAccountDetails) {
		PaymentRequest paymentRequest = this.paymentRequestRepository
			.findByPaymentId(requestDto.getPaymentDetails().getId());
		PaymentDetails existingDetails = this.paymentDetailsRepository.findById(requestDto.getPaymentDetails().getId())
			.orElseThrow(() -> new EntityNotFoundException("PaymentDetails not found"));
		paymentRequest.setPaymentDetails(existingDetails);
		paymentRequest.setStatus(PI.name());
		paymentRequest.setRePreferenceFlag(PaymentRequestEntityUtil.resolveRePreferenceFlag(requestDto));
		this.createStSeSegmentAndSet(requestDto, subAccountDetails, paymentRequest);
		paymentRequest.setUpdatedOn(LocalDateTime.now());
		this.paymentRequestRepository.save(paymentRequest);
		this.logUtility.logMessage("Payment request updated successfully", INFO, log);
		savePaymentRequestHistory(paymentRequest);
	}

	private void savePaymentRequestHistory(PaymentRequest paymentRequest) {
		// save payment request history
		PaymentRequestHistory paymentRequestHistory = new PaymentRequestHistory();

		paymentRequestHistory.setCorrelationId(paymentRequest.getCorrelationId());
		paymentRequestHistory.setIssuerPaymentId(paymentRequest.getPaymentDetails().getIssuerPaymentId());
		paymentRequestHistory.setReferenceId(paymentRequest.getPaymentDetails().getReferenceId());
		paymentRequestHistory.setRemitMethod(paymentRequest.getPaymentDetails().getRemitMethod());
		paymentRequestHistory.setAmount(paymentRequest.getPaymentDetails().getAmount());
		paymentRequestHistory.setStatus(paymentRequest.getStatus());
		paymentRequestHistory.setStSeSegment(paymentRequest.getStSeSegment());
		paymentRequestHistory.setSubmitterId(paymentRequest.getSubmitterId());
		paymentRequestHistory.setOrigin(paymentRequest.getOrigin());
		paymentRequestHistory.setPayr835id(paymentRequest.getPayr835Id());
		paymentRequestHistory.setOrgShortCode(paymentRequest.getOrgShortCode());
		paymentRequestHistory.setCreatedById(paymentRequest.getCreatedById());
		paymentRequestHistory.setAccountShortCode(paymentRequest.getAccountShortCode());
		paymentRequestHistory.setSubAccountShortCode(paymentRequest.getSubAccountShortCode());
		paymentRequestHistory.setTotalClmCount(paymentRequest.getTotalClmCount());
		paymentRequestHistory.setClmUrl(paymentRequest.getClmUrl());
		paymentRequestHistory.setDocType(paymentRequest.getDocType());
		paymentRequestHistory.setDocuments(paymentRequest.getDocuments());
		paymentRequestHistory.setMetadata(paymentRequest.getMetadata());
		paymentRequestHistory.setRePreferenceFlag(paymentRequest.isRePreferenceFlag());
		paymentRequestHistory.setCreatedOn(LocalDateTime.now());
		paymentRequestHistory.setPaymentDetailsId(paymentRequest.getPaymentDetails().getId());
		paymentRequestHistory.setErrorMessage(paymentRequest.getErrorMessage());
		this.paymentRequestHistoryRepository.save(paymentRequestHistory);
		this.logUtility.logMessage("Payment request history saved successfully", INFO, log);
	}

	private void createStSeSegmentAndSet(PaymentRequestDto requestDto, SubAccountBankAccountDetails subAccountDetails,
			PaymentRequest paymentRequest) {
		try {
			String stseSegment = this.segmentCreationService.createSegment(requestDto, subAccountDetails,
					(REMITTANCE_ONLY.equals(requestDto.getPaymentDetails().getType())
							? paymentRequest.getCreatedOn().toLocalDate() : null),
					paymentRequest.getPaymentDetails().getNetworkPaymentId());
			paymentRequest.setStSeSegment(stseSegment);
			paymentRequest.setStatus(PI.name());
			paymentRequest.setErrorMessage(null);
		}
		catch (Exception ex) {
			String errorMsg = "Error while creating STSE segment for payment Id:"
					+ requestDto.getPaymentDetails().getId() + " " + AppConstants.ERROR_MESSAGE + " " + ex.getMessage();
			this.logUtility.logMessageWithPriority(errorMsg, ERROR, log, P3);
			paymentRequest.setStatus(PE.name());
			paymentRequest.setErrorMessage(appendErrorMessage(paymentRequest.getErrorMessage(), errorMsg));
			this.paymentRequestRepository.save(paymentRequest);
			savePaymentRequestHistory(paymentRequest);
			throw new SegmentGenerationException(errorMsg, ex);
		}
	}

	private SubAccountBankAccountDetails fetchSubAccountDetails(PaymentRequestDto requestDto) {
		return this.subAccountRepository.findSubAccountBankDetails(requestDto.getOrgShortCode(),
				requestDto.getAccountShortCode(), requestDto.getSubAccountShortCode());
	}

	private SpendRules getGlobalRules(SpendRules spendRules) {
		try {
			ResponseEntity<String> response = this.zetaApiService.getAllRules(1, Integer.MAX_VALUE, null);
			String body = response.getBody();
			if (body == null || body.isEmpty()) {
				this.logUtility.logMessage("Empty response body received from getAllRules API", INFO, log);
				return spendRules;
			}
			if (!response.getStatusCode().is2xxSuccessful()) {
				this.logUtility.logMessage("Error occurred while fetching global rules api ", AppConstants.ERROR, log);
				return spendRules;
			}
			RulesResponse rulesResponse = JSON.fromJson(response.getBody(), RulesResponse.class);
			List<String> requiredRuleIds = rulesResponse.getData()
				.stream()
				.filter((rule) -> "Required".equalsIgnoreCase(rule.getRuleType()))
				.map((ruleId) -> ruleId.getRuleID())
				.collect(Collectors.toList());

			if (!requiredRuleIds.isEmpty()) {
				if (spendRules != null) {
					this.logUtility.logMessage("Existing spend rules found : " + spendRules.getRuleCode(), INFO, log);
					spendRules.getRuleCode().addAll(requiredRuleIds);
				}
				else {
					this.logUtility.logMessage(
							"No existing spend rules found. Creating new spend rules with global required rules: "
									+ requiredRuleIds,
							INFO, log);
					spendRules = new SpendRules();
					spendRules.setRuleCode(requiredRuleIds);
				}
			}
			else {
				this.logUtility.logMessage("No global required rules found in Zeta response", INFO, log);
			}

			return spendRules;
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Error while fetching rules from Zeta: " + ex.getMessage(), ERROR, log);
			return spendRules;
		}
	}

	private void updatePaymentRequestWithError(String paymentId, String errorMessage) {
		try {
			PaymentRequest paymentRequest = this.paymentRequestRepository.findByPaymentId(paymentId);
			if (paymentRequest != null) {
				paymentRequest.setStatus(PE.name());
				paymentRequest.setErrorMessage(appendErrorMessage(paymentRequest.getErrorMessage(), errorMessage));
				paymentRequest.setUpdatedOn(LocalDateTime.now());
				paymentRequest.setUpdatedById(VIRTUAL_CARD);
				this.paymentRequestRepository.save(paymentRequest);
				savePaymentRequestHistory(paymentRequest);
				this.logUtility.logMessage(
						"Payment request updated with PE status and error message for paymentId: " + paymentId, INFO,
						log);
			}
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority("Failed to update payment request with error status for paymentId: "
					+ paymentId + " - " + ex.getMessage(), ERROR, log, P3);
		}
	}

	/**
	 * Validates CardHolderDto to ensure no disallowed characters or invalid data from
	 * database are sent to external APIs (Zeta).
	 * @param cardHolder the CardHolderDto to validate
	 * @param paymentId the payment ID for logging purposes
	 * @throws BadRequestException if validation fails
	 */
	private void validateCardHolderDto(CardHolderDto cardHolder, String paymentId) {
		Set<ConstraintViolation<CardHolderDto>> violations = this.validator.validate(cardHolder);
		if (!violations.isEmpty()) {
			String violationMessages = violations.stream()
				.map((v) -> v.getPropertyPath() + ": " + v.getMessage())
				.collect(Collectors.joining("; "));
			String errorMsg = "Invalid cardholder data for paymentId: " + paymentId + " - " + violationMessages;
			// Log all violations for debugging purposes (no PHI/PII)
			this.logUtility.logMessage(
					String.format("BadRequest: Invalid cardholder data detected for paymentId: %s | Violations: %s",
							paymentId, violationMessages),
					ERROR, log);

			throw new BadRequestException(errorMsg);
		}
	}

	/**
	 * Validates SpendRules to ensure no disallowed characters or data exceeding 255
	 * characters from database are sent to external APIs (Zeta).
	 * @param spendRules the SpendRules to validate
	 * @param paymentId the payment ID for logging purposes
	 * @throws BadRequestException if validation fails
	 */
	private void validateSpendRules(SpendRules spendRules, String paymentId) {
		if (spendRules == null || spendRules.getRuleCode() == null) {
			return; // No validation needed for null spend rules
		}

		// Regex for disallowed characters: < > & " ' \ / and control characters (U+0000
		// to U+001F)
		String disallowedCharsRegex = "[<>&\"'\\\\/<\u0000-\u001F]";

		for (int i = 0; i < spendRules.getRuleCode().size(); i++) {
			String ruleCode = spendRules.getRuleCode().get(i);

			if (ruleCode == null || ruleCode.isEmpty()) {
				continue; // Skip null/empty rule codes
			}

			// Check length (max 255 characters)
			if (ruleCode.length() > 255) {
				String errorMsg = String.format(
						"Invalid spend rule at index %d for paymentId: %s - Exceeds 255 characters (length: %d)", i,
						paymentId, ruleCode.length());
				// Log only index and failure type (no sensitive rule values)
				this.logUtility.logMessage("BadRequest: Invalid spend rule detected for paymentId: " + paymentId
						+ " | Rule index: " + i + " | Failure type: Exceeds 255 characters", ERROR, log);
				throw new BadRequestException(errorMsg);
			}

			// Check for disallowed characters
			if (ruleCode.matches(".*" + disallowedCharsRegex + ".*")) {
				String errorMsg = String.format(
						"Invalid spend rule at index %d for paymentId: %s - Contains disallowed characters (< > & \" ' \\ / and control characters)",
						i, paymentId);
				// Log only index and failure type (no sensitive rule values)
				this.logUtility.logMessage("BadRequest: Invalid spend rule detected for paymentId: " + paymentId
						+ " | Rule index: " + i + " | Failure type: Contains disallowed characters", ERROR, log);
				throw new BadRequestException(errorMsg);
			}
		}
	}

}
