package com.uhg.optum.financials.payments.payer.service.handler;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.PaymentNetworkMetadataDto;
import com.uhg.optum.financials.payments.payer.enums.ActivationStatus;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.enums.CardOperationStatus;
import com.uhg.optum.financials.payments.payer.enums.CompletionStage;
import com.uhg.optum.financials.payments.payer.enums.VirtualCardState;
import com.uhg.optum.financials.payments.payer.exception.ServiceUnavailableException;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.ReprocessLockRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.ConsentDecisionService;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.service.StpRemittanceService;
import com.uhg.optum.financials.payments.payer.service.SummaryPageService;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.AmountConverter;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import com.uhg.optum.financials.payments.payer.util.PaymentRequestDtoUtil;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import com.uhg.optum.financials.payments.payer.util.WebhookRequestUtil;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ACTIVE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD_CREATION_STATUS_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_CONVERTING_DTO_TO_JSON;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_DETAILS_NOT_FOUND_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.Constants.COMPLETED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.DOT_DELIMITER;
import static com.uhg.optum.financials.payments.payer.constant.Constants.ISSUED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.LOADING;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.CARD_ISSUED;
import static com.uhg.optum.financials.payments.payer.enums.Status.PE;
import static com.uhg.optum.financials.payments.payer.enums.Status.PI;

@Slf4j
@Transactional
@Service
@RequiredArgsConstructor
public class CreateCardWebhookHandler {

	private static final String AUTO_ACTIVATED_ON_CREATE = "AUTO_ACTIVATED_ON_CREATE";

	private static final String CONSENT_REQUIRED_PENDING = "CONSENT_REQUIRED_PENDING";

	private final CardCreationRepository cardCreationRepository;

	private final LogUtility logUtility;

	private final AuditLogger auditLogger;

	private final VirtualCardsRepository virtualCardsRepository;

	private final VirtualCardUtil virtualCardUtil;

	private final ZetaApiService zetaApiService;

	private final EncryptionAtRestService encryptionAtRestService;

	private final WebhookClient webhookClient;

	private final ModelConversionMapper cardDetailsMapper;

	private final SummaryPageService summaryPageService;

	private final StpRemittanceService stpRemittanceService;

	private final ConsentDecisionService consentDecisionService;

	private final PaymentRequestDtoUtil paymentRequestDtoUtil;

	private final ReprocessLockRepository reprocessLockRepository;

	ObjectMapper mapper = new ObjectMapper();

	@Value("${endpoint.issuing.loadingNotification}")
	private String loadingNotificationEndpoint;

	@Value("${endpoint.issuing.cardEventNotification}")
	private String cardIssuedNotificationEndpoint;

	private final PaymentRequestRepository paymentRequestRepository;

	public void process(CreateCardData createCardData) {
		this.logUtility.logMessage(
				String.format("[requestId=%s] [cardGuid=%s] [WebhookReceived] started processing create card webhook.",
						createCardData.getClientRequestId(), createCardData.getCardGuid()),
				INFO, log);

		CardCreation cardCreation = null;
		try {
			validateCreateCardData(createCardData);

			cardCreation = updateCardCreationStatus(createCardData);
			PaymentRequest paymentRequest = this.paymentRequestRepository.findByPaymentId(cardCreation.getPaymentId());
			CompletionStage stage = CompletionStage.valueOf(cardCreation.getCompletionStage());

			if (stage == CompletionStage.COMPLETED) {
				// Already fully processed on a prior run — a redelivered/duplicate
				// webhook
				// message for this payment needs no further work.
				this.logUtility.logMessage("Card creation already COMPLETED for paymentId: "
						+ cardCreation.getPaymentId() + ", skipping reprocessing.", INFO, log);
				return;
			}

			VirtualCard virtualCard;
			if (stage.isBefore(CompletionStage.VIRTUAL_CARD_CREATED)) {
				if (createCardData.getAutoActivateCard() == null) {
					// A reprocess-reconstructed event doesn't carry this — it isn't
					// persisted anywhere, so re-derive it the same way the original
					// submission did, now that paymentRequest (with payeeDetails) is
					// loaded.
					createCardData.setAutoActivateCard(this.consentDecisionService
						.determineAutoActivateCard(this.paymentRequestDtoUtil.mapToDto(paymentRequest)));
				}
				CardDetailsDTO cardDetailsDTO = fetchAndSaveCardDetails(createCardData);
				virtualCard = saveVirtualCardCreate(createCardData, cardCreation, paymentRequest,
						cardDetailsDTO.getMaskedPan());
				cardCreation.setCompletionStage(CompletionStage.VIRTUAL_CARD_CREATED.name());
				this.cardCreationRepository.save(cardCreation);

				this.logUtility.logMessage(CARD_CREATION_STATUS_MESSAGE + cardCreation.getStatus(), INFO, log);
				this.auditLogger.logCardCreated(cardCreation.getId(), createCardData.getCardGuid(),
						cardCreation.getPaymentId());
				this.logUtility.logMessage("Card details saved for cardGuid: " + createCardData.getCardGuid(), INFO,
						log);
			}
			else {
				// Already created on a prior run — don't call Zeta/insert again, just
				// pick up the existing row so later stages have what they need.
				virtualCard = this.virtualCardsRepository.findByPaymentId(cardCreation.getPaymentId());
			}

			if (CARD_ISSUED.name().equalsIgnoreCase(cardCreation.getStatus())) {
				if (paymentRequest.getPaymentDetails() == null) {
					throw new IllegalArgumentException(PAYMENT_DETAILS_NOT_FOUND_ERROR + cardCreation.getPaymentId());
				}

				if (stage.isBefore(CompletionStage.COMPLETION_WEBHOOK_SENT)) {
					this.logUtility.logMessage(
							"Sending loading.completed webhook for paymentId: " + cardCreation.getPaymentId(), INFO,
							log);
					boolean webhookSent = sendCompletedWebhook(paymentRequest, createCardData.getCardGuid(),
							virtualCard.getInitialBalance(), virtualCard.getCurrentBalance(),
							virtualCard.getAvailableBalance(), String.valueOf(cardCreation.getBin()),
							virtualCard.getMaskedPan(), paymentRequest.getPaymentDetails().getNetworkPaymentId(),
							virtualCard.getExpiryDate());
					if (!webhookSent) {
						// Do not advance completion_stage on a failed delivery — leaving
						// it unset is what lets a reprocess attempt retry sending this
						// webhook instead of permanently skipping it.
						throw new ServiceUnavailableException(
								"Failed to deliver loading.completed webhook for paymentId: "
										+ cardCreation.getPaymentId(),
								null);
					}
					cardCreation.setCompletionStage(CompletionStage.COMPLETION_WEBHOOK_SENT.name());
					this.cardCreationRepository.save(cardCreation);
				}

				if (stage.isBefore(CompletionStage.DOCUMENT_GENERATED)) {
					this.summaryPageService.processSummaryDocumentMessage(paymentRequest, cardCreation.getExpiryDate(),
							virtualCard.getCardGuid());
					cardCreation.setCompletionStage(CompletionStage.DOCUMENT_GENERATED.name());
					this.cardCreationRepository.save(cardCreation);
				}

				if (stage.isBefore(CompletionStage.STP_COMPLETED)) {
					// Isolate STP remittance failures to prevent blocking card issuance
					// completion
					// STP has its own lifecycle/history, so failures here should not
					// prevent sendCompletedWebhook
					try {
						this.stpRemittanceService.sendRemittance(paymentRequest);
					}
					catch (Exception stpException) {
						log.error("STP remittance failed for paymentId: {}. Error: {}", cardCreation.getPaymentId(),
								stpException.getMessage());
						this.logUtility.logMessageWithPriority(String.format(
								"STP remittance failed for paymentId=%s: %s. Continuing with card issuance completion.",
								cardCreation.getPaymentId(), stpException.getMessage()), ERROR, log, P3);
						appendStpErrorToPaymentRequest(paymentRequest, stpException);
					}
					cardCreation.setCompletionStage(CompletionStage.STP_COMPLETED.name());
					this.cardCreationRepository.save(cardCreation);
				}

				// Reprocessing starts from a PE payment; once every completion stage has
				// succeeded, the payment needs to rejoin the normal flow so the
				// downstream settlement batch job (which only picks up PI payments) can
				// process it like any other successfully issued card. Routed through the
				// shared utility so this transition is recorded in
				// payment_request_history like every other status change here.
				if (PE.name().equals(paymentRequest.getStatus())) {
					this.virtualCardUtil.updatePaymentRequestStatus(cardCreation.getPaymentId(), PI,
							"Payment successfully reprocessed for paymentId: " + cardCreation.getPaymentId());
				}

				if (stage.isBefore(CompletionStage.COMPLETED)) {
					cardCreation.setCompletionStage(CompletionStage.COMPLETED.name());
					this.cardCreationRepository.save(cardCreation);
				}
			}
		}
		catch (Exception ex) {
			handleProcessException(cardCreation, createCardData, ex);
		}
		finally {
			// Held for the entire duration of processing, not just until it starts — a
			// reconstructed resume event only ever reaches this method with a valid,
			// already-persisted CardCreation, so cardCreation is non-null whenever this
			// lock was actually acquired by a reprocess trigger. Harmless no-op for a
			// normal (non-reprocessed) webhook, which never acquired it.
			if (cardCreation != null) {
				this.reprocessLockRepository.releaseLock("payment:" + cardCreation.getPaymentId());
			}
		}
		this.logUtility.logMessage(
				String.format("[requestId=%s] [cardGuid=%s] [WebhookReceived] finished processing create card webhook.",
						createCardData.getClientRequestId(), createCardData.getCardGuid()),
				INFO, log);
	}

	private void validateCreateCardData(CreateCardData createCardData) {
		if (createCardData.getClientRequestId() == null) {
			throw new IllegalArgumentException("RequestId cannot be null in CreateCardData");
		}
	}

	private CardCreation updateCardCreationStatus(CreateCardData createCardData) {
		CardCreation cardCreation = this.virtualCardUtil
			.getCardCreationByRequestId(String.valueOf(createCardData.getClientRequestId()));
		cardCreation.setIsSingleSpend(createCardData.getSingleSpend()); // check if we
																		// need to save
																		// from the zeta
																		// webhook and
																		// raise alert
		cardCreation.setStatus(CardOperationStatus.CARD_ISSUED.name());
		cardCreation.setCardGuid(createCardData.getCardGuid());
		// Only initialize on first receipt of this webhook — a resumed/reprocessed call
		// must not reset progress already recorded by a prior partial run.
		if (cardCreation.getCompletionStage() == null) {
			cardCreation.setCompletionStage(CompletionStage.ISSUANCE_CONFIRMED.name());
		}
		cardCreation.setUpdatedAt(LocalDateTime.now());
		cardCreation.setUpdatedBy(VIRTUAL_CARD);
		cardCreation.setCompletedAt(LocalDateTime.now());
		cardCreation.setCompletedBy(VIRTUAL_CARD);
		return cardCreation;
	}

	private CardDetailsDTO fetchAndSaveCardDetails(CreateCardData createCardData) throws Exception {
		CardDetailsDTO cardDetailsDTO = this.zetaApiService.getCardDetails(createCardData.getCardGuid());
		if (cardDetailsDTO == null) {
			throw new RuntimeException("Card details not found for cardGuid: " + createCardData.getCardGuid());
		}
		return cardDetailsDTO;
	}

	private void handleProcessException(CardCreation cardCreation, CreateCardData createCardData, Exception ex) {
		this.auditLogger.logException(((cardCreation != null) ? cardCreation.getId() : null), null,
				((cardCreation != null) ? cardCreation.getPaymentId() : "UNKNOWN"), null, CardEventType.CREATE, ex);
		this.logUtility.logMessage("Error while fetching card details for cardGuid: " + createCardData.getCardGuid()
				+ "- error: " + ex.getMessage(), ERROR, log);
		this.logUtility.logMessageWithPriority("Virtual card creation failed for requestId: "
				+ createCardData.getClientRequestId() + ERROR_MESSAGE + ex.getMessage(), ERROR, log, P3);
		this.virtualCardUtil.updatePaymentRequestStatus(((cardCreation != null) ? cardCreation.getPaymentId() : null),
				PE, ex.getMessage());
	}

	private VirtualCard saveVirtualCardCreate(CreateCardData createCardData, CardCreation cardCreation,
			PaymentRequest paymentRequest, String maskedPan) {
		VirtualCard existing = this.virtualCardsRepository.findByPaymentId(cardCreation.getPaymentId());
		if (existing != null) {
			// Already created on a prior run — return as-is. Balances are mutated
			// independently by auth/clear processing, so this must never overwrite an
			// existing row.
			return existing;
		}

		VirtualCard virtualCard = new VirtualCard();
		// Must equal the CardCreation.id of the attempt that actually succeeded, to
		// satisfy the composite fk_virtual_cards_request foreign key on (payment_id, id).
		virtualCard.setId(cardCreation.getId());
		virtualCard.setCardGuid(createCardData.getCardGuid());
		virtualCard.setPaymentId(cardCreation.getPaymentId());
		virtualCard.setAvailableBalance(AmountConverter.toDollars(createCardData.getInitialBalance().getAmount()));
		virtualCard.setInitialBalance(AmountConverter.toDollars(createCardData.getInitialBalance().getAmount()));
		virtualCard.setCurrentBalance(AmountConverter.toDollars(createCardData.getInitialBalance().getAmount()));
		virtualCard.setCurrency(createCardData.getInitialBalance().getCurrency());
		virtualCard.setStatus(VirtualCardState.CREATED.name());
		virtualCard.setProviderTin(resolveProviderTin(paymentRequest));
		virtualCard.setProviderState(resolveProviderState(paymentRequest));
		virtualCard.setExpiryDate(cardCreation.getExpiryDate());
		virtualCard.setSpend_rules(createCardData.getSpendRules());
		virtualCard.setActivationReason(resolveActivationReason(createCardData));
		virtualCard.setActivationStatusUpdatedAt(LocalDateTime.now());
		virtualCard.setCreatedBy(VIRTUAL_CARD);
		virtualCard.setCreatedAt(LocalDateTime.now());
		virtualCard.setMaskedPan(maskedPan(maskedPan));
		virtualCard.setActivationStatus(activationStatus(createCardData));
		virtualCard = this.virtualCardsRepository.save(virtualCard);
		this.virtualCardUtil.saveVirtualCardHistory(virtualCard, "CREATED");
		this.logUtility.logMessage("Virtual card status set to: " + virtualCard.getStatus(), INFO, log);
		return virtualCard;
	}

	private String resolveProviderTin(PaymentRequest paymentRequest) {
		if (paymentRequest == null || paymentRequest.getPayeeDetails() == null
				|| paymentRequest.getPayeeDetails().getTaxId() == null) {
			return null;
		}

		return paymentRequest.getPayeeDetails().getTaxId();
	}

	private String resolveProviderState(PaymentRequest paymentRequest) {
		if (paymentRequest == null || paymentRequest.getPayeeDetails() == null
				|| paymentRequest.getPayeeDetails().getAddress() == null
				|| paymentRequest.getPayeeDetails().getAddress().getState() == null) {
			return null;
		}

		return paymentRequest.getPayeeDetails().getAddress().getState().trim().toUpperCase();
	}

	public String activationStatus(CreateCardData createCardData) {
		return Boolean.TRUE.equals(createCardData.getAutoActivateCard()) ? ActivationStatus.ACTIVE.name()
				: ActivationStatus.INACTIVE.name();
	}

	private String resolveActivationReason(CreateCardData createCardData) {
		return Boolean.TRUE.equals(createCardData.getAutoActivateCard()) ? AUTO_ACTIVATED_ON_CREATE
				: CONSENT_REQUIRED_PENDING;
	}

	private void sendCardIssuedWebhook(PaymentRequest paymentRequest, VirtualCard virtualCard, String bin, String last4,
			String issuerPaymentId) {
		this.logUtility.logMessage(
				"Sending card.issued webhook for paymentId: " + paymentRequest.getPaymentDetails().getId(), INFO, log);

		CardEventWebhookBaseDto cardIssuedDto = WebhookRequestUtil.getCardEventWebhookBaseDto(
				CARD + DOT_DELIMITER + ISSUED, paymentRequest.getPaymentDetails().getId(), paymentRequest.getMetadata(),
				issuerPaymentId);
		CardEventWebhookPayload cardIssuedWebhookPayload = WebhookRequestUtil.getCardEventWebhookPayload(bin, last4,
				cardIssuedDto.getType(), null, null, virtualCard, null, null, null, null, null);
		cardIssuedDto.setCard(cardIssuedWebhookPayload);
		try {
			String cardIssuedDtoJson = this.mapper.writerWithDefaultPrettyPrinter().writeValueAsString(cardIssuedDto);
			this.logUtility.logMessage("Sending card.issued webhook payload: " + cardIssuedDtoJson, INFO, log);
		}
		catch (JsonProcessingException ex) {
			this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
		}
		this.webhookClient.notifyClient(cardIssuedDto, paymentRequest.getOrgShortCode(),
				this.cardIssuedNotificationEndpoint);

	}

	private boolean sendCompletedWebhook(PaymentRequest paymentRequest, UUID cardGuid, BigDecimal initialBalance,
			BigDecimal currentBalance, BigDecimal availableBalance, String bin, String pan, String networkPaymentId,
			LocalDate expiryDate) {

		LoadingWebhookRequestDto sentLoadingDto = (LoadingWebhookRequestDto) WebhookRequestUtil
			.getLoadingWebhookBaseDto(LOADING + DOT_DELIMITER + COMPLETED, paymentRequest.getPaymentDetails().getId(),
					paymentRequest.getMetadata(), null, null, networkPaymentId);
		PaymentNetworkMetadataDto paymentNetworkMetadata = WebhookRequestUtil.getPaymentNetworkMetaData(cardGuid,
				initialBalance, currentBalance, availableBalance, bin, pan, null, ACTIVE);
		LoadingWebhookRequestPayload loadingSentWebhookRequestPayload = WebhookRequestUtil
			.getLoadingWebhookRequestPayload(0.0, LOADING + DOT_DELIMITER + COMPLETED,
					"Loading " + COMPLETED + " Successfully",
					paymentRequest.getPaymentDetails().getAmount().doubleValue(), expiryDate);
		sentLoadingDto.setData(loadingSentWebhookRequestPayload);
		sentLoadingDto.setPaymentNetworkMetadata(paymentNetworkMetadata);
		try {
			String sentLoadingDtoJson = this.mapper.writerWithDefaultPrettyPrinter().writeValueAsString(sentLoadingDto);
			this.logUtility.logMessage("Sending loading.completed  webhook payload: " + sentLoadingDtoJson, INFO, log);
		}
		catch (JsonProcessingException ex) {
			this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
		}
		// notifyClient() swallows delivery failures after exhausting its own retries and
		// always returns normally — notifyClientWithStatus() is the variant that
		// actually reports whether delivery succeeded, which the caller needs in order
		// to decide whether completion_stage may advance.
		return this.webhookClient.notifyClientWithStatus(sentLoadingDto, paymentRequest.getOrgShortCode(),
				this.loadingNotificationEndpoint);
	}

	private String maskedPan(String maskedPan) {
		if (maskedPan == null || maskedPan.isEmpty()) {
			return "****";
		}
		String cleanedPan = maskedPan.replace("-", "");
		if (cleanedPan.length() > 4) {
			return cleanedPan.substring(0, cleanedPan.length() - 4).replaceAll(".", "*")
					+ cleanedPan.substring(cleanedPan.length() - 4);
		}
		else {
			// Prepend with '*' to make the total length 4
			return String.format("%4s", cleanedPan).replace(' ', '*');
		}
	}

	private void appendStpErrorToPaymentRequest(PaymentRequest paymentRequest, Exception stpException) {
		if (paymentRequest == null) {
			return;
		}
		try {
			String stpErrorMessage = "STP remittance failed: " + stpException.getMessage();
			String existingErrorMessage = paymentRequest.getErrorMessage();
			if (existingErrorMessage == null || existingErrorMessage.isBlank()) {
				paymentRequest.setErrorMessage(stpErrorMessage);
			}
			else {
				paymentRequest.setErrorMessage(existingErrorMessage + System.lineSeparator() + stpErrorMessage);
			}
			paymentRequest.setUpdatedById(VIRTUAL_CARD);
			paymentRequest.setUpdatedOn(LocalDateTime.now());
			this.paymentRequestRepository.save(paymentRequest);
		}
		catch (Exception ex) {
			String paymentId = (paymentRequest.getPaymentDetails() != null) ? paymentRequest.getPaymentDetails().getId()
					: "UNKNOWN";
			this.logUtility.logMessage("Failed to append STP error message into paymentRequest for paymentId: "
					+ paymentId + ERROR_MESSAGE + ex.getMessage(), ERROR, log);
		}
	}

}
