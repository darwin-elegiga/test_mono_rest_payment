package com.uhg.optum.financials.payments.payer.service.handler;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.dto.callback.CancelCardData;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.PaymentNetworkMetadataDto;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.enums.CardOperationStatus;
import com.uhg.optum.financials.payments.payer.enums.VirtualCardState;
import com.uhg.optum.financials.payments.payer.model.*;
import com.uhg.optum.financials.payments.payer.repository.CardCancellationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
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

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_CONVERTING_DTO_TO_JSON;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.IN_ACTIVE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VOIDED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD_CANCELLATION_STATUS_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_DETAILS_NOT_FOUND_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD_NOT_FOUND_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.Constants.DOT_DELIMITER;
import static com.uhg.optum.financials.payments.payer.constant.Constants.LOADING;
import static com.uhg.optum.financials.payments.payer.constant.Constants.PAYMENT;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.CARD_CANCELLED;
import static com.uhg.optum.financials.payments.payer.enums.Status.PV;

@Slf4j
@Transactional
@Service
@RequiredArgsConstructor
public class CancelCardWebhookHandler {

	private final CardCancellationRepository cardCancellationRepository;

	private final LogUtility logUtility;

	private final AuditLogger auditLogger;

	private final VirtualCardsRepository virtualCardsRepository;

	private final PaymentDetailsRepository paymentDetailsRepository;

	private final VirtualCardUtil virtualCardUtil;

	@Value("${endpoint.issuing.loadingNotification}")
	private String cardVoidedNotificationEndpoint;

	private final WebhookClient webhookClient;

	ObjectMapper mapper = new ObjectMapper();

	public void process(CancelCardData cancelCardData) {
		this.logUtility.logMessage(
				String.format("[requestId=%s] [cardGuid=%s] [WebhookReceived] started processing cancel card webhook.",
						cancelCardData.getClientRequestId(), cancelCardData.getCardGuid()),
				INFO, log);
		VirtualCard virtualCard = null;
		try {
			var clientRequestId = cancelCardData.getClientRequestId();
			var cardCreation = this.virtualCardUtil.getCardCreationByRequestId(String.valueOf(clientRequestId));

			if (java.util.Objects.equals(cardCreation.getStatus(), AppConstants.CARD_ISSUE_FAILED)) {
				this.logUtility.logMessage(String.format(
						"[requestId=%s] [cardGuid=%s] [WebhookReceived] Card creation failed for requestId: %s. Skipping cancel card processing.",
						cancelCardData.getClientRequestId(), cancelCardData.getCardGuid(), clientRequestId), INFO, log);
				return;
			}

			CardCancellation cardCancellation = this.cardCancellationRepository
				.findById(cancelCardData.getClientRequestId())
				.orElseThrow(() -> new IllegalArgumentException(
						"CardCancellation not found for requestId: " + cancelCardData.getClientRequestId()));
			virtualCard = this.virtualCardsRepository.findByCardGuid(cancelCardData.getCardGuid())
				.orElseThrow(() -> new IllegalArgumentException(
						VIRTUAL_CARD_NOT_FOUND_ERROR + cancelCardData.getCardGuid()));
			cardCancellation.setStatus(CardOperationStatus.CARD_CANCELLED.name());
			cardCancellation.setCompletedAt(LocalDateTime.now());
			cardCancellation.setCompletedBy(VIRTUAL_CARD);
			cardCancellation.setReasonCode(cancelCardData.getReasonCode());
			this.cardCancellationRepository.save(cardCancellation);
			this.logUtility.logMessage(CARD_CANCELLATION_STATUS_MESSAGE + cardCancellation.getStatus(), INFO, log);
			this.auditLogger.logCardCancelled(cardCancellation.getId(), cardCancellation.getCardGuid(),
					cardCancellation.getPaymentId());
			saveVirtualCardCancel(cardCancellation, VirtualCardState.CANCELLED.name());
			PaymentRequest paymentRequest = this.virtualCardUtil
				.updatePaymentRequestStatus(cardCancellation.getPaymentId(), PV);
			if (CARD_CANCELLED.name().equalsIgnoreCase(cardCancellation.getStatus())) {
				this.logUtility.logMessage("Sending card voided webhook for cardId: " + cardCancellation.getCardGuid(),
						INFO, log);
				PaymentDetails paymentDetails = this.paymentDetailsRepository.findById(cardCancellation.getPaymentId())
					.orElseThrow(() -> new IllegalArgumentException(
							PAYMENT_DETAILS_NOT_FOUND_ERROR + cardCancellation.getPaymentId()));

				sendVoidedWebhook(paymentRequest, cancelCardData.getCardGuid(), virtualCard.getInitialBalance(),
						virtualCard.getCurrentBalance(), virtualCard.getAvailableBalance(), null, null,
						paymentDetails.getNetworkPaymentId(), cardCancellation.getReasonCode(),
						virtualCard.getExpiryDate());
			}
		}
		catch (Exception ex) {
			this.auditLogger.logException(cancelCardData.getClientRequestId(), cancelCardData.getCardGuid(),
					((virtualCard != null) ? virtualCard.getPaymentId() : "UNKNOWN"), null, CardEventType.CANCEL, ex);
			this.logUtility.logMessage("Error while processing cancel card for cardGuid: "
					+ cancelCardData.getCardGuid() + " - " + ex.getMessage(), ERROR, log);
			this.logUtility.logMessageWithPriority("Virtual card void failed for cardGuid: "
					+ cancelCardData.getCardGuid() + ERROR_MESSAGE + ex.getMessage(), ERROR, log, P3);
		}
		this.logUtility.logMessage(String.format(
				"[requestId=%s] [cardGuid=%s] [WebhookReceived] completed processing cancel card webhook.",
				cancelCardData.getClientRequestId(), cancelCardData.getCardGuid()), INFO, log);
	}

	private void saveVirtualCardCancel(CardCancellation cardCancellation, String status) {
		VirtualCard virtualCard = this.virtualCardsRepository.findByPaymentId(cardCancellation.getPaymentId());
		if (virtualCard != null) {
			virtualCard.setStatus(status);
			virtualCard.setUpdatedBy(VIRTUAL_CARD);
			virtualCard.setUpdatedAt(LocalDateTime.now());
			this.virtualCardsRepository.save(virtualCard);
			this.virtualCardUtil.saveVirtualCardHistory(virtualCard, cardCancellation.getReasonCode());
		}
		else {
			this.logUtility.logMessage("VirtualCard not found for paymentId: " + cardCancellation.getPaymentId(), ERROR,
					log);
			throw new IllegalArgumentException(
					"VirtualCard not found for paymentId: " + cardCancellation.getPaymentId());
		}
	}

	public void sendVoidedWebhook(PaymentRequest paymentRequest, UUID cardGuid, BigDecimal initialBalance,
			BigDecimal currentBalance, BigDecimal availableBalance, String bin, String pan, String networkPaymentId,
			String reasonCode, LocalDate expiryDate) {

		LoadingWebhookRequestDto sentLoadingDto = (LoadingWebhookRequestDto) WebhookRequestUtil
			.getLoadingWebhookBaseDto(LOADING + DOT_DELIMITER + PAYMENT + DOT_DELIMITER + VOIDED,
					paymentRequest.getPaymentDetails().getId(), paymentRequest.getMetadata(), null, null,
					networkPaymentId);
		LoadingWebhookRequestPayload loadingSentWebhookRequestPayload = WebhookRequestUtil
			.getLoadingWebhookRequestPayload(0.0, LOADING + DOT_DELIMITER + PAYMENT + DOT_DELIMITER + VOIDED,
					"Loading " + VOIDED + " Successfully", 0.0, expiryDate);

		// Business Rule: ReasonCode should always be empty for loading.voided webhook.
		// Changing this may break upstream processing , please do not change
		loadingSentWebhookRequestPayload.getPaymentActivity().setReasonCode("");
		sentLoadingDto.setData(loadingSentWebhookRequestPayload);
		PaymentNetworkMetadataDto paymentNetworkMetadata = WebhookRequestUtil.getPaymentNetworkMetaData(cardGuid,
				initialBalance, BigDecimal.ZERO, BigDecimal.ZERO, bin, pan, null, IN_ACTIVE);
		sentLoadingDto.setPaymentNetworkMetadata(paymentNetworkMetadata);
		try {
			String sentLoadingDtoJson = this.mapper.writerWithDefaultPrettyPrinter().writeValueAsString(sentLoadingDto);
			this.logUtility.logMessage("Sending loading.voided webhook payload: " + sentLoadingDtoJson, INFO, log);
		}
		catch (JsonProcessingException ex) {
			this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
		}
		this.webhookClient.notifyClient(sentLoadingDto, paymentRequest.getOrgShortCode(),
				this.cardVoidedNotificationEndpoint);
	}

}
