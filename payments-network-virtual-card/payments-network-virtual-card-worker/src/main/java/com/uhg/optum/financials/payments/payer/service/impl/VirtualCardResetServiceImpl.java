package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.dto.ResponseObject;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.exception.CardProcessingError;
import com.uhg.optum.financials.payments.payer.model.CardReset;
import com.uhg.optum.financials.payments.payer.repository.CardResetRepository;
import com.uhg.optum.financials.payments.payer.service.VirtualCardResetService;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.CardOperationLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.WebhookAlertUtil;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD_RESET_STATUS_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.RESET_ERROR_MSG;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.UNEXPECTED_RESPONSE_STATUS;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.Constants.RESET;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_ACCEPTED;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_INITIATED;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_REJECTED;

@Service
@Slf4j
public class VirtualCardResetServiceImpl implements VirtualCardResetService {

	private final LogUtility logUtility;

	private final ZetaApiService zetaApiService;

	private final CardResetRepository cardResetRepository;

	private final AuditLogger auditLogger;

	private final CardOperationLogger cardOperationLogger;

	private final WebhookAlertUtil webhookAlertUtil;

	public VirtualCardResetServiceImpl(LogUtility logUtility, ZetaApiService zetaApiService,
			CardResetRepository cardResetRepository, AuditLogger auditLogger, CardOperationLogger cardOperationLogger,
			WebhookAlertUtil webhookAlertUtil) {
		this.logUtility = logUtility;
		this.zetaApiService = zetaApiService;
		this.cardResetRepository = cardResetRepository;
		this.auditLogger = auditLogger;
		this.cardOperationLogger = cardOperationLogger;
		this.webhookAlertUtil = webhookAlertUtil;
	}

	@Override
	public void processResetCardRequest(ResetCardRequest resetCardRequest) {
		this.logUtility.logMessage("reset virtual card with request: " + resetCardRequest, INFO, log);
		CardReset cardReset = saveResetCardRequest(resetCardRequest);
		sendCardResetRequestToZeta(resetCardRequest, cardReset);
	}

	private CardReset saveResetCardRequest(ResetCardRequest resetCardRequest) {
		this.logUtility.logMessage("Saving resetCardRequest to DB", INFO, log);
		CardReset cardReset = new CardReset();
		cardReset.setId(UUID.randomUUID());
		cardReset.setCardGuid(resetCardRequest.getCardGuid());
		cardReset.setResetPurpose(resetCardRequest.getResetPurpose());
		cardReset.setReceiptId(resetCardRequest.getReceiptId());
		cardReset.setStatus(REQUEST_INITIATED.name());
		cardReset.setCreatedAt(LocalDateTime.now());
		return this.cardResetRepository.save(cardReset);
	}

	private void sendCardResetRequestToZeta(ResetCardRequest resetCardRequest, CardReset cardReset) {
		this.cardOperationLogger.logCardProcessStarted(CardEventType.RESET, cardReset.getPaymentId(),
				cardReset.getReceiptId(), resetCardRequest, cardReset.getStatus(), log);
		ResponseEntity<ResponseObject> response = null;
		try {
			this.auditLogger.logResetInitiated(cardReset.getId(), resetCardRequest.getPaymentId(),
					resetCardRequest.getReceiptId());
			response = (ResponseEntity<ResponseObject>) this.zetaApiService.resetVirtualCard(resetCardRequest);
			if (response != null && response.getStatusCode().is2xxSuccessful()) {
				cardReset.setStatus(REQUEST_ACCEPTED.name());
				cardReset.setUpdatedAt(LocalDateTime.now());
				cardReset.setUpdatedBy(VIRTUAL_CARD);
				this.logUtility.logMessage("Card reset response received with status: " + response + " | "
						+ CARD_RESET_STATUS_MESSAGE + cardReset.getStatus(), INFO, log);
				this.cardResetRepository.save(cardReset);
				this.auditLogger.logResetAccepted(cardReset.getId(), resetCardRequest.getPaymentId(),
						resetCardRequest.getReceiptId());
				this.webhookAlertUtil.scheduleTimeout(cardReset.getId(), RESET);
			}
			else if (response != null
					&& (response.getStatusCode().is4xxClientError() || response.getStatusCode().is5xxServerError())) {
				cardReset.setStatus(REQUEST_REJECTED.name());
				cardReset.setErrorCode(response.getStatusCode().toString());
				String errorMessage = response.getStatusCode()
						+ ((response.getBody() != null && response.getBody().getMessage() != null)
								? " - " + response.getBody().getMessage() : "");
				cardReset.setErrorMessage(errorMessage);
				cardReset.setUpdatedAt(LocalDateTime.now());
				cardReset.setUpdatedBy(VIRTUAL_CARD);
				this.logUtility.logMessage("Card reset response received with status: " + response + " | "
						+ CARD_RESET_STATUS_MESSAGE + cardReset.getStatus(), INFO, log);
				this.cardResetRepository.save(cardReset);
				this.auditLogger.logResetRejected(cardReset.getId(), resetCardRequest.getPaymentId(),
						resetCardRequest.getReceiptId());
				this.logUtility.logMessageWithPriority("Reset request failed for paymentId: " + cardReset.getPaymentId()
						+ ERROR_MESSAGE + errorMessage, ERROR, log, P3);
			}
			else {
				this.cardOperationLogger.logCardProcessError(CardEventType.RESET, cardReset.getPaymentId(),
						cardReset.getReceiptId(), response, log);
				this.auditLogger.logError(null, resetCardRequest.getPaymentId(), resetCardRequest.getReceiptId(),
						CardEventType.RESET, this.cardOperationLogger.extractResponseMessage(response));
				this.logUtility.logMessageWithPriority("Reset request failed for paymentId: " + cardReset.getPaymentId()
						+ ERROR_MESSAGE + this.cardOperationLogger.extractResponseMessage(response), ERROR, log, P3);
				throw new RuntimeException(UNEXPECTED_RESPONSE_STATUS);
			}
		}
		catch (CardProcessingError cpe) {
			this.logUtility.logMessage("Unexpected exception occurred: " + cpe.getMessage(), INFO, log);
			throw cpe;
		}
		catch (Exception ex) {
			this.cardOperationLogger.logCardProcessException(CardEventType.RESET, cardReset.getPaymentId(),
					cardReset.getReceiptId(), ex, log);
			this.auditLogger.logException(null, cardReset.getCardGuid(), resetCardRequest.getPaymentId(),
					resetCardRequest.getReceiptId(), CardEventType.RESET, ex);
			this.logUtility.logMessageWithPriority("Reset request failed for paymentId: " + cardReset.getPaymentId()
					+ ERROR_MESSAGE + this.cardOperationLogger.extractResponseMessage(response), ERROR, log, P3);
			throw new RuntimeException(RESET_ERROR_MSG + ex.getMessage());
		}
		this.cardOperationLogger.logCardProcessCompleted(CardEventType.RESET, cardReset.getPaymentId(),
				cardReset.getReceiptId(), this.cardOperationLogger.extractResponseMessage(response),
				cardReset.getStatus(), log);
	}

}
