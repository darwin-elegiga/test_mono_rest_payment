package com.uhg.optum.financials.payments.payer.service.handler;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.dto.callback.ResetCardData;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookPayload;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.enums.CardOperationStatus;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.CardReset;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardResetRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.service.impl.VirtualCardBalanceService;
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
import java.time.LocalDateTime;
import java.util.Optional;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_CONVERTING_DTO_TO_JSON;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_DETAILS_NOT_FOUND_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD_NOT_FOUND_ERROR;
import static com.uhg.optum.financials.payments.payer.constant.Constants.AUTH;
import static com.uhg.optum.financials.payments.payer.constant.Constants.DOT_DELIMITER;
import static com.uhg.optum.financials.payments.payer.constant.Constants.REVERSED;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.AUTH_RESET;

@Slf4j
@Transactional
@Service
@RequiredArgsConstructor
public class ResetCardWebhookHandler {

	private final CardResetRepository cardResetRepository;

	private final LogUtility logUtility;

	private final AuditLogger auditLogger;

	private final VirtualCardsRepository virtualCardsRepository;

	private final VirtualCardUtil virtualCardUtil;

	private final VirtualCardBalanceService virtualCardBalanceService;

	private final CardCreationRepository cardCreationRepository;

	private final AuthExtractRepository authExtractRepository;

	private final WebhookClient webhookClient;

	@Value("${endpoint.issuing.cardEventNotification}")
	private String cardAuthReversedNotificationEndpoint;

	ObjectMapper mapper = new ObjectMapper();

	public void process(ResetCardData resetCardData) {
		logStart(resetCardData);
		VirtualCard virtualCard = null;
		CardReset cardReset = null;

		try {
			cardReset = fetchLatestCardReset(resetCardData);
			if (cardReset == null) {
				handleMissingCardReset(resetCardData);
			}
			virtualCard = fetchVirtualCard(resetCardData.getCardGuid());
			cardReset = updateCardReset(cardReset, resetCardData, virtualCard.getPaymentId());
			if (AUTH_RESET.name().equalsIgnoreCase(cardReset.getStatus())) {
				AuthExtract authExtract = fetchAuthExtract(cardReset.getReceiptId());
				Long bin = resolveBin(authExtract, virtualCard);

				PaymentRequest paymentRequest = updatePaymentRequestStatus(virtualCard.getPaymentId());
				validatePaymentDetails(paymentRequest, virtualCard);

				handleAuthReset(paymentRequest, virtualCard, bin, authExtract, cardReset.getReceiptId());

				this.auditLogger.logCardReset(cardReset.getId(), resetCardData.getCardGuid(),
						virtualCard.getPaymentId(), resetCardData.getReceiptId());

				authExtract.setClearingStatus("AUTH_RESET_CLEAR");
				this.authExtractRepository.save(authExtract);
				logEnd(resetCardData);
			}
		}
		catch (Exception ex) {
			this.auditLogger.logException(((cardReset != null) ? cardReset.getId() : null), resetCardData.getCardGuid(),
					((virtualCard != null) ? virtualCard.getPaymentId() : "UNKNOWN"), resetCardData.getReceiptId(),
					CardEventType.RESET, ex);
			this.logUtility.logMessage("Error while processing reset card for cardGuid: " + resetCardData.getCardGuid()
					+ " - " + ex.getMessage(), ERROR, log);
			this.logUtility.logMessageWithPriority("Virtual card reset failed for receiptId: "
					+ resetCardData.getReceiptId() + ERROR_MESSAGE + ex.getMessage(), ERROR, log, P3);
		}
	}

	private void logStart(ResetCardData resetCardData) {
		this.logUtility.logMessage(
				String.format("[requestId=%s] [cardGuid=%s] [WebhookReceived] started processing reset card webhook.",
						resetCardData.getReceiptId(), resetCardData.getCardGuid()),
				INFO, log);
	}

	private CardReset fetchLatestCardReset(ResetCardData resetCardData) {
		return this.cardResetRepository.findTopByReceiptIdAndCardGuidOrderByCreatedAtDesc(resetCardData.getReceiptId(),
				resetCardData.getCardGuid());
	}

	private void handleMissingCardReset(ResetCardData resetCardData) {
		this.auditLogger.logCardResetFailed(null, resetCardData.getCardGuid(), null, resetCardData.getReceiptId());
		this.logUtility.logMessage("CardReset not found for cardGuid: " + resetCardData.getCardGuid(), ERROR, log);
		throw new IllegalArgumentException("CardReset not found for cardGuid: " + resetCardData.getCardGuid());
	}

	private VirtualCard fetchVirtualCard(UUID cardGuid) {
		return this.virtualCardsRepository.findByCardGuid(cardGuid)
			.orElseThrow(() -> new IllegalArgumentException(VIRTUAL_CARD_NOT_FOUND_ERROR + cardGuid));
	}

	private AuthExtract fetchAuthExtract(String receiptId) {
		AuthExtract authExtract = this.authExtractRepository.findByReceiptId(receiptId);
		if (authExtract == null) {
			this.logUtility.logMessage("AuthExtract not found for receiptId: " + receiptId, ERROR, log);
			throw new IllegalArgumentException("AuthExtract not found for receiptId: " + receiptId);
		}
		return authExtract;
	}

	private Long resolveBin(AuthExtract authExtract, VirtualCard virtualCard) {
		Long bin = Long.valueOf(authExtract.getBin());
		if (bin == null || bin == 0L) {
			CardCreation cardCreation = this.cardCreationRepository
				.findTopByPaymentIdOrderByCreatedAtDesc(virtualCard.getPaymentId())
				.orElseThrow(() -> new IllegalArgumentException(
						"CardCreation not found for paymentId: " + virtualCard.getPaymentId()));
			bin = cardCreation.getBin();
		}
		return bin;
	}

	private PaymentRequest updatePaymentRequestStatus(String paymentId) {
		return this.virtualCardUtil.updatePaymentRequestStatus(paymentId, null);
	}

	private void validatePaymentDetails(PaymentRequest paymentRequest, VirtualCard virtualCard) {
		if (paymentRequest.getPaymentDetails() == null) {
			throw new IllegalArgumentException(PAYMENT_DETAILS_NOT_FOUND_ERROR + virtualCard.getPaymentId());
		}
	}

	private CardReset updateCardReset(CardReset cardReset, ResetCardData resetCardData, String paymentId) {
		cardReset.setStatus(CardOperationStatus.AUTH_RESET.name());
		cardReset.setCompletedAt(LocalDateTime.now());
		cardReset.setCompletedBy(VIRTUAL_CARD);
		cardReset.setResetPurpose(resetCardData.getResetPurpose());
		cardReset.setPaymentId(paymentId);
		this.cardResetRepository.save(cardReset);
		return cardReset;
	}

	private void handleAuthReset(PaymentRequest paymentRequest, VirtualCard virtualCard, Long bin,
			AuthExtract authExtract, String receiptId) {
		this.logUtility.logMessage("Sending card.auth.reversed webhook for paymentId: " + virtualCard.getPaymentId(),
				INFO, log);

		this.virtualCardBalanceService.updateBalanceForAuthorization(virtualCard,
				Optional.ofNullable(authExtract.getAuthAmount()).orElse(BigDecimal.ZERO), AppConstants.CREDIT,
				receiptId);

		sendAuthReversedWebhook(paymentRequest, virtualCard, bin, getLast4Digits(authExtract.getMaskedCardNumber()),
				authExtract.getReceiptId(), authExtract.getAuthAmount(),
				paymentRequest.getPaymentDetails().getIssuerPaymentId());
	}

	private String getLast4Digits(String maskedCardNumber) {
		return maskedCardNumber.substring(maskedCardNumber.length() - 4);
	}

	private void logEnd(ResetCardData resetCardData) {
		this.logUtility.logMessage(
				String.format("[requestId=%s] [cardGuid=%s] [WebhookReceived] completed processing reset card webhook.",
						resetCardData.getReceiptId(), resetCardData.getCardGuid()),
				INFO, log);
	}

	private void sendAuthReversedWebhook(PaymentRequest paymentRequest, VirtualCard virtualCard, Long bin, String pan,
			String receiptId, BigDecimal authAmount, String issuerPaymentId) {
		this.logUtility.logMessage(
				"Sending card.auth.reversed webhook for paymentId: " + paymentRequest.getPaymentDetails().getId(), INFO,
				log);

		CardEventWebhookBaseDto cardEventWebhookBaseDto = WebhookRequestUtil.getCardEventWebhookBaseDto(
				CARD + DOT_DELIMITER + AUTH + DOT_DELIMITER + REVERSED, paymentRequest.getPaymentDetails().getId(),
				paymentRequest.getMetadata(), issuerPaymentId);
		CardEventWebhookPayload cardEventWebhookPayload = WebhookRequestUtil.getCardEventWebhookPayload(
				String.valueOf(bin), pan, cardEventWebhookBaseDto.getType(), receiptId, authAmount, virtualCard, null,
				null, null, null, null);
		cardEventWebhookBaseDto.setCard(cardEventWebhookPayload);
		try {
			String cardEventWebhookBaseDtoJson = this.mapper.writerWithDefaultPrettyPrinter()
				.writeValueAsString(cardEventWebhookBaseDto);
			this.logUtility.logMessage("Sending card.auth.reversed webhook payload: " + cardEventWebhookBaseDtoJson,
					INFO, log);
		}
		catch (JsonProcessingException ex) {
			this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
		}
		this.webhookClient.notifyClient(cardEventWebhookBaseDto, paymentRequest.getOrgShortCode(),
				this.cardAuthReversedNotificationEndpoint);
	}

}
