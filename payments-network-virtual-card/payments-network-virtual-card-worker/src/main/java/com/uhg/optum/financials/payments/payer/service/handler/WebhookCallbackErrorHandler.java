package com.uhg.optum.financials.payments.payer.service.handler;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.constant.Constants;
import com.uhg.optum.financials.payments.payer.constant.EventTypeConstants;
import com.uhg.optum.financials.payments.payer.dto.callback.CallbackError;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookPayload;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.LoadingWebhookRequestPayload;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.CardCancellation;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.CardReset;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCancellationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardResetRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
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

import java.time.LocalDateTime;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_CONVERTING_DTO_TO_JSON;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_DETAILS_NOT_FOUND_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD_NOT_FOUND_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.Constants.DOT_DELIMITER;
import static com.uhg.optum.financials.payments.payer.constant.Constants.FAILED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.LOADING;
import static com.uhg.optum.financials.payments.payer.constant.Constants.PAYMENT;
import static com.uhg.optum.financials.payments.payer.constant.Constants.REVERSED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.VOID;
import static com.uhg.optum.financials.payments.payer.enums.Status.PE;

@Slf4j
@Transactional
@Service
@RequiredArgsConstructor
public class WebhookCallbackErrorHandler {

	private final CardResetRepository cardResetRepository;

	private final LogUtility logUtility;

	private final AuditLogger auditLogger;

	private final VirtualCardsRepository virtualCardsRepository;

	private final PaymentDetailsRepository paymentDetailsRepository;

	private final VirtualCardUtil virtualCardUtil;

	private final CardCreationRepository cardCreationRepository;

	private final AuthExtractRepository authExtractRepository;

	private final WebhookClient webhookClient;

	private final CardCancellationRepository cardCancellationRepository;

	@Value("${endpoint.issuing.loadingNotification}")
	private String loadingNotificationEndpoint;

	@Value("${endpoint.issuing.cardEventNotification}")
	private String cardAuthReversedNotificationEndpoint;

	ObjectMapper mapper = new ObjectMapper();

	private final PaymentRequestRepository paymentRequestRepository;

	public void process(CallbackError error, String eventType) {
		if (eventType.equalsIgnoreCase(EventTypeConstants.VIRTUAL_CARD_CREATE)) {
			this.logUtility.logMessage(String.format(
					"[requestId=%s] [WebhookReceived] started processing callback error for CREATE card operation.",
					error.getClientRequestId()), INFO, log);

			CardCreation cardCreation = this.virtualCardUtil
				.getCardCreationByRequestId(String.valueOf(error.getClientRequestId()));
			if (cardCreation != null) {
				cardCreation.setStatus(AppConstants.CARD_ISSUE_FAILED);
				cardCreation.setErrorCode(error.getCode());
				cardCreation.setErrorMessage(error.getMessage());
				cardCreation.setUpdatedAt(LocalDateTime.now());
				cardCreation.setUpdatedBy(VIRTUAL_CARD);
				this.cardCreationRepository.save(cardCreation);
			}
			this.auditLogger.logCreateCallbackFailure(cardCreation.getId(), cardCreation.getPaymentId());
			this.logUtility.logMessageWithPriority("Virtual card creation failed for requestId: "
					+ error.getClientRequestId() + ERROR_MESSAGE + error.getMessage(), ERROR, log, "P3");
			this.logUtility.logMessage("Sending loading.failed webhook for paymentId: " + cardCreation.getPaymentId(),
					INFO, log);

			PaymentRequest paymentRequest = this.virtualCardUtil.updatePaymentRequestStatus(cardCreation.getPaymentId(),
					PE, error.getMessage());

			if (paymentRequest.getPaymentDetails() == null) {
				throw new IllegalArgumentException(PAYMENT_DETAILS_NOT_FOUND_ERROR + cardCreation.getPaymentId());
			}

			LoadingWebhookRequestDto deniedLoadingDto = (LoadingWebhookRequestDto) WebhookRequestUtil
				.getLoadingWebhookBaseDto(LOADING + DOT_DELIMITER + FAILED, paymentRequest.getPaymentDetails().getId(),
						paymentRequest.getMetadata(), error.getMessage(), error.getCode(),
						paymentRequest.getPaymentDetails().getNetworkPaymentId());
			LoadingWebhookRequestPayload networkPayload = WebhookRequestUtil.getLoadingWebhookRequestPayload(0.0, "",
					"", 0.0, null);
			deniedLoadingDto.setData(networkPayload);
			try {
				String deniedLoadingDtoJson = this.mapper.writerWithDefaultPrettyPrinter()
					.writeValueAsString(deniedLoadingDto);
				this.logUtility.logMessage("Sending loading.failed webhook payload: " + deniedLoadingDtoJson, INFO,
						log);
			}
			catch (JsonProcessingException ex) {
				this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
			}
			this.webhookClient.notifyClient(deniedLoadingDto, paymentRequest.getOrgShortCode(),
					this.loadingNotificationEndpoint);
			this.logUtility.logMessage(String.format(
					"[requestId=%s] [WebhookReceived] completed processing callback error for CREATE card operation.",
					error.getClientRequestId()), INFO, log);
		}
		else if (eventType.equalsIgnoreCase(EventTypeConstants.VIRTUAL_CARD_CANCEL)) {
			this.logUtility.logMessage(String.format(
					"[requestId=%s] [WebhookReceived] started processing callback error for CANCEL card operation.",
					error.getClientRequestId()), INFO, log);
			CardCancellation cardCancellation = this.cardCancellationRepository.findById(error.getClientRequestId())
				.orElseThrow(() -> new IllegalArgumentException(
						"CardCancellation not found for requestId: " + error.getClientRequestId()));
			cardCancellation.setStatus(AppConstants.CARD_CANCELLATION_FAILED);
			cardCancellation.setErrorCode(error.getCode());
			cardCancellation.setErrorMessage(error.getMessage());
			cardCancellation.setUpdatedAt(LocalDateTime.now());
			cardCancellation.setUpdatedBy(VIRTUAL_CARD);
			this.cardCancellationRepository.save(cardCancellation);
			this.auditLogger.logCancelCallbackFailure(cardCancellation.getId(), cardCancellation.getCardGuid(),
					cardCancellation.getPaymentId());
			this.logUtility.logMessageWithPriority("Virtual card void failed for requestId: "
					+ error.getClientRequestId() + ERROR_MESSAGE + error.getMessage(), ERROR, log, "P3");
			this.logUtility.logMessage(
					"Sending loading.payment.void.failed webhook for cardId: " + cardCancellation.getCardGuid(), INFO,
					log);

			PaymentDetails paymentDetails = this.paymentDetailsRepository.findById(cardCancellation.getPaymentId())
				.orElseThrow(() -> new IllegalArgumentException(
						PAYMENT_DETAILS_NOT_FOUND_ERROR + cardCancellation.getPaymentId()));

			PaymentRequest paymentRequest = this.paymentRequestRepository
				.findByPaymentId(cardCancellation.getPaymentId());
			LoadingWebhookRequestDto deniedLoadingVoidDto = (LoadingWebhookRequestDto) WebhookRequestUtil
				.getLoadingWebhookBaseDto(
						LOADING + DOT_DELIMITER + PAYMENT + DOT_DELIMITER + VOID + DOT_DELIMITER + FAILED,
						paymentRequest.getPaymentDetails().getId(), paymentRequest.getMetadata(), error.getMessage(),
						error.getCode(), paymentRequest.getPaymentDetails().getNetworkPaymentId());
			LoadingWebhookRequestPayload networkPayload = WebhookRequestUtil.getLoadingWebhookRequestPayload(0.0, "",
					"", 0.0, null);
			deniedLoadingVoidDto.setData(networkPayload);
			try {
				String deniedLoadingVoidDtoJson = this.mapper.writerWithDefaultPrettyPrinter()
					.writeValueAsString(deniedLoadingVoidDto);
				this.logUtility.logMessage(
						"Sending loading.payment.void.failed webhook payload: " + deniedLoadingVoidDtoJson, INFO, log);
			}
			catch (JsonProcessingException ex) {
				this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
			}
			this.webhookClient.notifyClient(deniedLoadingVoidDto, paymentRequest.getOrgShortCode(),
					this.loadingNotificationEndpoint);
			this.logUtility.logMessage(String.format(
					"[requestId=%s] [WebhookReceived] completed processing callback error for CANCEL card operation.",
					error.getClientRequestId()), INFO, log);
		}
		else if (eventType.equalsIgnoreCase(EventTypeConstants.VIRTUAL_CARD_RESET)) {
			this.logUtility.logMessage(String.format(
					"[requestId=%s] [WebhookReceived] started processing callback error for RESET card operation.",
					error.getClientRequestId()), INFO, log);
			CardReset cardReset = this.cardResetRepository.findByReceiptId(error.getReceiptId());
			if (cardReset == null) {
				throw new IllegalArgumentException("Card not found for receiptId: " + error.getReceiptId());
			}
			else {
				cardReset.setStatus(AppConstants.AUTH_RESET_FAILED);
				cardReset.setErrorCode(error.getCode());
				cardReset.setErrorMessage(error.getMessage());
				cardReset.setUpdatedAt(LocalDateTime.now());
				cardReset.setUpdatedBy(VIRTUAL_CARD);
				this.cardResetRepository.save(cardReset);
				VirtualCard virtualCard = this.virtualCardsRepository.findByCardGuid(cardReset.getCardGuid())
					.orElseThrow(
							() -> new IllegalArgumentException(VIRTUAL_CARD_NOT_FOUND_ERROR + cardReset.getCardGuid()));
				this.auditLogger.logResetCallbackFailure(cardReset.getId(), virtualCard.getPaymentId(),
						cardReset.getReceiptId());
				this.logUtility.logMessageWithPriority("Virtual card auth reverse failed for receiptId: "
						+ error.getReceiptId() + ERROR_MESSAGE + error.getMessage(), ERROR, log, "P3");
				this.logUtility.logMessage(
						"Sending card.auth.reverse.failed webhook for cardId: " + cardReset.getCardGuid(), INFO, log);

				PaymentDetails paymentDetails = this.paymentDetailsRepository.findById(virtualCard.getPaymentId())
					.orElseThrow(() -> new IllegalArgumentException(
							PAYMENT_DETAILS_NOT_FOUND_ERROR + virtualCard.getPaymentId()));
				PaymentRequest paymentRequest = this.paymentRequestRepository
					.findByPaymentId(virtualCard.getPaymentId());
				CardEventWebhookBaseDto cardAuthReversedFailedDto = WebhookRequestUtil.getCardEventWebhookBaseDto(
						CARD + DOT_DELIMITER + Constants.AUTH + DOT_DELIMITER + REVERSED + DOT_DELIMITER + FAILED,
						virtualCard.getPaymentId(), null, paymentDetails.getNetworkPaymentId());
				CardCreation cardCreation = this.cardCreationRepository
					.findTopByPaymentIdOrderByCreatedAtDesc(virtualCard.getPaymentId())
					.orElseThrow(() -> new IllegalArgumentException(
							"CardCreation not found for paymentId: " + virtualCard.getPaymentId()));
				AuthExtract cardByReceiptId = this.authExtractRepository.findByReceiptId(cardReset.getReceiptId());
				if (cardByReceiptId == null) {
					throw new IllegalArgumentException(
							"VirtualCard not found for ReceiptId: " + cardReset.getReceiptId());
				}
				CardEventWebhookPayload cardEventWebhookPayload = WebhookRequestUtil.getCardEventWebhookPayload(
						String.valueOf(cardCreation.getBin()),
						(cardByReceiptId.getMaskedCardNumber() != null
								&& cardByReceiptId.getMaskedCardNumber().length() >= 4)
										? cardByReceiptId.getMaskedCardNumber()
											.substring(cardByReceiptId.getMaskedCardNumber().length() - 4)
										: null,
						cardAuthReversedFailedDto.getType(), cardByReceiptId.getReceiptId(),
						cardByReceiptId.getAuthAmount(), virtualCard, null, null, null, null, error.getMessage());
				cardAuthReversedFailedDto.setCard(cardEventWebhookPayload);
				try {
					String cardAuthReversedFailedDtoJson = this.mapper.writerWithDefaultPrettyPrinter()
						.writeValueAsString(cardAuthReversedFailedDto);
					this.logUtility.logMessage(
							"Sending card.auth.reverse.failed webhook payload: " + cardAuthReversedFailedDtoJson, INFO,
							log);
				}
				catch (JsonProcessingException ex) {
					this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
				}

				this.webhookClient.notifyClient(cardAuthReversedFailedDto, paymentRequest.getOrgShortCode(),
						this.cardAuthReversedNotificationEndpoint);
				this.logUtility.logMessage(String.format(
						"[requestId=%s] [WebhookReceived] completed processing callback error for RESET card operation.",
						error.getClientRequestId()), INFO, log);
			}
		}
		else {
			this.auditLogger.logError(null, null, null, getCardEventType(eventType), error.getMessage());
			this.logUtility.logMessage("Unsupported Zeta callback type: " + eventType, ERROR, log);
			throw new IllegalArgumentException("Unsupported Zeta callback type: " + eventType);
		}
	}

	private CardEventType getCardEventType(String eventType) {
		if (EventTypeConstants.VIRTUAL_CARD_CREATE.equalsIgnoreCase(eventType)) {
			return CardEventType.CREATE;
		}
		else if (EventTypeConstants.VIRTUAL_CARD_CANCEL.equalsIgnoreCase(eventType)) {
			return CardEventType.CANCEL;
		}
		else if (EventTypeConstants.VIRTUAL_CARD_RESET.equalsIgnoreCase(eventType)) {
			return CardEventType.RESET;
		}
		else {
			return null;
		}
	}

}
