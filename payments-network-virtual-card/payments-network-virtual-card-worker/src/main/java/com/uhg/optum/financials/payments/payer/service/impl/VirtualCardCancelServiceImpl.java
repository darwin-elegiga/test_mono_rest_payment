package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.payments.payer.dto.CancelCardRequest;
import com.uhg.optum.financials.payments.payer.dto.ResponseObject;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.exception.CardProcessingError;
import com.uhg.optum.financials.payments.payer.model.CardCancellation;
import com.uhg.optum.financials.payments.payer.repository.CardCancellationRepository;
import com.uhg.optum.financials.payments.payer.service.VirtualCardCancelService;
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

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CANCELLATION_ERROR_MSG;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CARD_CANCELLATION_STATUS_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.OTHER;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD_CANCELLATION_REQUEST_FAILED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.CANCEL;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_ACCEPTED;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_INITIATED;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_REJECTED;

@Slf4j
@Service
public class VirtualCardCancelServiceImpl implements VirtualCardCancelService {

	private final LogUtility logUtility;

	private final ZetaApiService zetaApiService;

	private final CardCancellationRepository cardCancellationRepository;

	private final AuditLogger auditLogger;

	private final CardOperationLogger cardOperationLogger;

	private final WebhookAlertUtil webhookAlertUtil;

	public VirtualCardCancelServiceImpl(LogUtility logUtility, ZetaApiService zetaApiService,
			CardCancellationRepository cardCancellationRepository, AuditLogger auditLogger,
			CardOperationLogger cardOperationLogger, WebhookAlertUtil webhookAlertUtil) {
		this.logUtility = logUtility;
		this.zetaApiService = zetaApiService;
		this.cardCancellationRepository = cardCancellationRepository;
		this.auditLogger = auditLogger;
		this.cardOperationLogger = cardOperationLogger;
		this.webhookAlertUtil = webhookAlertUtil;
	}

	@Override
	public void processCancelCardRequest(CancelCardDto cancelCardDto) {
		this.logUtility.logMessage("Cancel virtual card with request: " + cancelCardDto, INFO, log);
		CancelCardRequest cancelCardRequest = createCardCancellationRequest(cancelCardDto);
		CardCancellation cardCancellation = saveCardCancellationRequest(cancelCardRequest, cancelCardDto);
		sendCardCancellationRequestToZeta(cancelCardRequest, cardCancellation);

	}

	private CancelCardRequest createCardCancellationRequest(CancelCardDto cancelCardDto) {
		CancelCardRequest cancelCardRequest = new CancelCardRequest();
		cancelCardRequest.setRequestId(UUID.randomUUID());
		cancelCardRequest.setReasonCode(OTHER);
		cancelCardRequest.setCardGuid(cancelCardDto.getCardGuid());
		return cancelCardRequest;
	}

	private CardCancellation saveCardCancellationRequest(CancelCardRequest cancelCardRequest,
			CancelCardDto cancelCardDto) {
		this.logUtility.logMessage("Saving cancelCardRequest to DB", INFO, log);
		CardCancellation cardCancellation = new CardCancellation();
		cardCancellation.setId(cancelCardRequest.getRequestId());
		cardCancellation.setPaymentId(cancelCardDto.getPaymentId());
		cardCancellation.setCardGuid(cancelCardRequest.getCardGuid());
		cardCancellation.setStatus(REQUEST_INITIATED.name());
		cardCancellation.setCreatedAt(LocalDateTime.now());
		cardCancellation.setCreatedBy(VIRTUAL_CARD);
		cardCancellation.setReasonCode(cancelCardRequest.getReasonCode());
		return this.cardCancellationRepository.save(cardCancellation);
	}

	private void sendCardCancellationRequestToZeta(CancelCardRequest cancelCardRequest,
			CardCancellation cardCancellation) {
		this.cardOperationLogger.logCardProcessStarted(CardEventType.CANCEL, cardCancellation.getPaymentId(),
				cancelCardRequest.getRequestId().toString(), cancelCardRequest, cardCancellation.getStatus(), log);
		ResponseEntity<ResponseObject> response = null;
		try {
			this.auditLogger.logCancelInitiated(cardCancellation.getId(), cancelCardRequest.getCardGuid(),
					cardCancellation.getPaymentId());
			response = (ResponseEntity<ResponseObject>) this.zetaApiService.cancelVirtualCard(cancelCardRequest);
			if (response != null && response.getStatusCode().is2xxSuccessful()) {
				cardCancellation.setStatus(REQUEST_ACCEPTED.name());
				cardCancellation.setUpdatedAt(LocalDateTime.now());
				cardCancellation.setUpdatedBy(VIRTUAL_CARD);
				this.logUtility.logMessage("Card cancellation response received with status: " + response + " | "
						+ CARD_CANCELLATION_STATUS_MESSAGE + cardCancellation.getStatus(), INFO, log);
				this.cardCancellationRepository.save(cardCancellation);
				this.auditLogger.logCancelAccepted(cardCancellation.getId(), cancelCardRequest.getCardGuid(),
						cardCancellation.getPaymentId());
				this.webhookAlertUtil.scheduleTimeout(cardCancellation.getId(), CANCEL);
			}
			else if (response != null
					&& (response.getStatusCode().is4xxClientError() || response.getStatusCode().is5xxServerError())) {

				cardCancellation.setStatus(REQUEST_REJECTED.name());
				cardCancellation.setErrorCode(VIRTUAL_CARD_CANCELLATION_REQUEST_FAILED);
				String errorMessage = response.getStatusCode()
						+ ((response.getBody() != null && response.getBody().getMessage() != null)
								? " - " + response.getBody().getMessage() : "");
				cardCancellation.setErrorMessage(errorMessage);
				cardCancellation.setUpdatedAt(LocalDateTime.now());
				cardCancellation.setUpdatedBy(VIRTUAL_CARD);
				this.logUtility.logMessage("Card cancellation response received with status: " + response + " | "
						+ CARD_CANCELLATION_STATUS_MESSAGE + cardCancellation.getStatus(), INFO, log);
				this.cardCancellationRepository.save(cardCancellation);
				this.auditLogger.logCancelRejected(cardCancellation.getId(), cancelCardRequest.getCardGuid(),
						cardCancellation.getPaymentId());
				this.logUtility.logMessageWithPriority("Cancel request failed for paymentId: "
						+ cardCancellation.getPaymentId() + ERROR_MESSAGE + errorMessage, ERROR, log, P3);
			}
			else {
				this.cardOperationLogger.logCardProcessError(CardEventType.CANCEL, cardCancellation.getPaymentId(),
						cancelCardRequest.getRequestId().toString(), response, log);
				this.auditLogger.logError(cardCancellation.getId(), cardCancellation.getPaymentId(), null,
						CardEventType.CANCEL, this.cardOperationLogger.extractResponseMessage(response));
				this.logUtility.logMessageWithPriority(
						"Cancel request failed for paymentId: " + cardCancellation.getPaymentId() + ERROR_MESSAGE
								+ this.cardOperationLogger.extractResponseMessage(response),
						ERROR, log, P3);
				throw new CardProcessingError("Unexpected response status from virtual card cancellation service");
			}
		}
		catch (CardProcessingError cpe) {
			this.logUtility.logMessage("Unexpected exception occurred: " + cpe.getMessage(), INFO, log);
			throw cpe;
		}
		catch (Exception ex) {
			this.cardOperationLogger.logCardProcessException(CardEventType.CANCEL, cardCancellation.getPaymentId(),
					cancelCardRequest.getRequestId().toString(), ex, log);
			this.auditLogger.logException(cardCancellation.getId(), cardCancellation.getCardGuid(),
					cardCancellation.getPaymentId(), null, CardEventType.CANCEL, ex);
			this.logUtility
				.logMessageWithPriority("Cancel request failed for paymentId: " + cardCancellation.getPaymentId()
						+ ERROR_MESSAGE + this.cardOperationLogger.extractResponseMessage(response), ERROR, log, P3);
			throw new RuntimeException(CANCELLATION_ERROR_MSG + ex.getMessage());
		}
		this.cardOperationLogger.logCardProcessCompleted(CardEventType.CANCEL, cardCancellation.getPaymentId(),
				cancelCardRequest.getRequestId().toString(), this.cardOperationLogger.extractResponseMessage(response),
				cardCancellation.getStatus(), log);
	}

}
