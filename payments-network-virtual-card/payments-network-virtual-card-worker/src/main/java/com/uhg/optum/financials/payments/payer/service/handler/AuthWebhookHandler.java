package com.uhg.optum.financials.payments.payer.service.handler;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.constant.EventTypeConstants;
import com.uhg.optum.financials.payments.payer.dto.authevent.AuthorizationPayload;
import com.uhg.optum.financials.payments.payer.dto.authevent.MerchantInfo;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookBaseDto;
import com.uhg.optum.financials.payments.payer.dto.webhook.CardEventWebhookPayload;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.OrphanEvents;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.OrphanEventsRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.service.impl.VirtualCardBalanceService;
import com.uhg.optum.financials.payments.payer.util.AmountConverter;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import com.uhg.optum.financials.payments.payer.util.WebhookRequestUtil;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.util.Optional;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.DEBUG;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_CONVERTING_DTO_TO_JSON;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.Constants.AUTH;
import static com.uhg.optum.financials.payments.payer.constant.Constants.DOT_DELIMITER;

@Slf4j
@Transactional
@Service
@RequiredArgsConstructor
public class AuthWebhookHandler {

	private final LogUtility logUtility;

	private final AuditLogger auditLogger;

	private final VirtualCardsRepository virtualCardsRepository;

	private final VirtualCardBalanceService virtualCardBalanceService;

	private final AuthExtractRepository authExtractRepository;

	private final WebhookClient webhookClient;

	private final ModelConversionMapper cardDetailsMapper;

	private final OrphanEventsRepository orphanEventsRepository;

	private final PaymentRequestRepository paymentRequestRepository;

	ObjectMapper mapper = new ObjectMapper();

	@Value("${endpoint.issuing.cardEventNotification}")
	private String cardAuthApprovedNotificationEndpoint;

	public void process(AuthorizationPayload authorizationPayload) throws JsonProcessingException {
		Optional<VirtualCard> virtualCardOptional = this.virtualCardsRepository
			.findByCardGuid(UUID.fromString(authorizationPayload.getCardId()));
		VirtualCard virtualCard = virtualCardOptional.orElse(null);

		if (virtualCard == null) {
			this.logUtility.logMessage("VirtualCard not found for cardGuid: " + authorizationPayload.getCardId(), ERROR,
					log);
			saveOrphanEvents(authorizationPayload);
			return;
		}

		try {
			if (EventTypeConstants.AUTH_EVENT_APPROVED.equalsIgnoreCase(authorizationPayload.getTransactionStatus())) {
				this.logUtility.logMessage("Processing auth event for cardGuid: " + authorizationPayload.getCardId(),
						INFO, log);
				AuthExtract authEventExtract = getAuthEventExtract(authorizationPayload);
				authEventExtract.setPaymentRequestId(authorizationPayload.getOriginalAuthRequestId());
				this.authExtractRepository.save(authEventExtract);
				this.virtualCardBalanceService.updateBalanceForAuthorization(virtualCard,
						AmountConverter.toDollars(Long.valueOf(authorizationPayload.getAuthAmount().getValue())),
						AppConstants.DEBIT, authEventExtract.getReceiptId());
				this.auditLogger.logAuthAccepted(virtualCard.getCardGuid(), virtualCard.getPaymentId(),
						authEventExtract.getReceiptId());
				this.logUtility.logMessage(
						"Auth approved event saved for cardGuid: " + authorizationPayload.getCardId(), INFO, log);
				sendAuthEventExtractNotification(authEventExtract, EventTypeConstants.AUTH_EVENT_APPROVED, virtualCard,
						authorizationPayload.getMerchantInfo());
			}
			else {
				this.logUtility.logMessage(
						"Auth declined event received for cardGuid: " + authorizationPayload.getCardId(), INFO, log);
				AuthExtract authEventExtract = getAuthEventExtract(authorizationPayload);
				authEventExtract.setPaymentRequestId(authorizationPayload.getOriginalAuthRequestId());
				this.authExtractRepository.save(authEventExtract);
				this.auditLogger.logAuthRejected(UUID.fromString(authorizationPayload.getCardId()),
						virtualCard.getPaymentId(), authEventExtract.getReceiptId());
				this.logUtility.logMessage(
						"Auth declined event saved for cardGuid: " + authorizationPayload.getCardId(), INFO, log);
				sendAuthEventExtractNotification(authEventExtract, EventTypeConstants.AUTH_EVENT_DECLINED, virtualCard,
						authorizationPayload.getMerchantInfo());
			}

		}
		catch (Exception ex) {
			this.auditLogger.logException(null, UUID.fromString(authorizationPayload.getCardId()),
					virtualCard.getPaymentId(), authorizationPayload.getReceiptId(), CardEventType.AUTH, ex);
			this.logUtility.logMessage("Error while processing auth event for cardGuid: "
					+ authorizationPayload.getCardId() + " - " + ex.getMessage(), ERROR, log);
		}
	}

	private AuthExtract getAuthEventExtract(AuthorizationPayload authorizationPayload) {
		AuthExtract authEventExtract = this.cardDetailsMapper.modelMapper(authorizationPayload, AuthExtract.class);

		BigDecimal convertedAuthAmount = AmountConverter.toDollars((authorizationPayload.getAuthAmount() != null
				&& authorizationPayload.getAuthAmount().getValue() != null)
						? Long.parseLong(authorizationPayload.getAuthAmount().getValue()) : 0L);
		authEventExtract.setAuthAmount(convertedAuthAmount);
		authEventExtract.setCreatedAt(LocalDateTime.now());
		authEventExtract.setCreatedBy(VIRTUAL_CARD);
		authEventExtract.setSystemAuthTime((StringUtils.isNotEmpty(authorizationPayload.getSystemAuthTime()))
				? OffsetDateTime.parse(authorizationPayload.getSystemAuthTime()) : OffsetDateTime.now());
		authEventExtract.setAuthExpiryTime(authorizationPayload.getAuthExpiryTime());

		return authEventExtract;
	}

	private void saveOrphanEvents(AuthorizationPayload authorizationPayload) throws JsonProcessingException {
		OrphanEvents orphanEvents = new OrphanEvents();
		orphanEvents.setCardId(UUID.fromString(authorizationPayload.getCardId()));
		orphanEvents.setReceiptId(authorizationPayload.getReceiptId());
		orphanEvents.setOriginalAuthRequestId(authorizationPayload.getOriginalAuthRequestId());
		orphanEvents.setEventPayload(new ObjectMapper().writeValueAsString(authorizationPayload));
		orphanEvents.setCreatedAt(LocalDateTime.now());
		orphanEvents.setEventType("INVALID_AUTH_EVENT");
		orphanEvents.setCreatedBy(VIRTUAL_CARD);
		this.orphanEventsRepository.save(orphanEvents);
		this.logUtility.logMessage("Saved fraud info for cardGuid: " + authorizationPayload.getCardId(), DEBUG, log);
	}

	private void sendAuthEventExtractNotification(AuthExtract transactionEventExtract, String eventStatus,
			VirtualCard virtualCard, MerchantInfo merchantInfo) {
		this.logUtility.logMessage("Sending " + CARD + DOT_DELIMITER + AUTH + DOT_DELIMITER + eventStatus.toLowerCase()
				+ " webhook for receiptId: " + transactionEventExtract.getReceiptId(), INFO, log);
		PaymentRequest paymentRequest = this.paymentRequestRepository.findByPaymentId(virtualCard.getPaymentId());
		CardEventWebhookBaseDto cardIssuedDto = WebhookRequestUtil.getCardEventWebhookBaseDto(
				CARD + DOT_DELIMITER + AUTH + DOT_DELIMITER + eventStatus.toLowerCase(), virtualCard.getPaymentId(),
				null, transactionEventExtract.getPaymentRequestId());
		CardEventWebhookPayload cardIssuedWebhookPayload = WebhookRequestUtil.getCardEventWebhookPayload(
				transactionEventExtract.getBin(), transactionEventExtract.getMaskedCardNumber(),
				cardIssuedDto.getType(), transactionEventExtract.getReceiptId(),
				transactionEventExtract.getAuthAmount(), virtualCard, merchantInfo,
				transactionEventExtract.getApprovalCode(), transactionEventExtract.getAuthExpiryTime(),
				transactionEventExtract.getResponseCode(), transactionEventExtract.getDeclineReason());
		cardIssuedDto.setCard(cardIssuedWebhookPayload);
		try {
			String authExtractJson = this.mapper.writerWithDefaultPrettyPrinter().writeValueAsString(cardIssuedDto);
			this.logUtility.logMessage("Sending " + CARD + DOT_DELIMITER + AUTH + DOT_DELIMITER
					+ eventStatus.toLowerCase() + " webhook for receiptId: " + authExtractJson, INFO, log);
		}
		catch (JsonProcessingException ex) {
			this.logUtility.logMessage(ERROR_CONVERTING_DTO_TO_JSON + ex.getMessage(), ERROR, log);
		}
		this.webhookClient.notifyClient(cardIssuedDto, paymentRequest.getOrgShortCode(),
				this.cardAuthApprovedNotificationEndpoint);
	}

}
