package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.enums.CardEventSource;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.enums.CardOperationStatus;
import com.uhg.optum.financials.payments.payer.model.AuditLog;
import com.uhg.optum.financials.payments.payer.repository.AuditLogRepository;
import lombok.RequiredArgsConstructor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import java.time.LocalDateTime;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;

@Component
@RequiredArgsConstructor
public class AuditLogger {

	private static final Logger log = LoggerFactory.getLogger(AuditLogger.class);

	private final AuditLogRepository auditLogRepository;

	private final LogUtility logUtility;

	private void logInternal(UUID requestId, UUID cardGuid, String paymentId, CardEventType eventType,
			CardOperationStatus status, CardEventSource source, String receiptId) {

		AuditLog auditLog = new AuditLog();
		auditLog.setRequestId(requestId);
		auditLog.setCardGuid(cardGuid);
		auditLog.setPaymentId(paymentId);
		auditLog.setEventType(eventType.name());
		auditLog.setStatus(status.name());
		auditLog.setSource(source.name());
		auditLog.setCreatedAt(LocalDateTime.now());
		auditLog.setCreatedBy(VIRTUAL_CARD);
		auditLog.setReceiptId(receiptId);

		this.auditLogRepository.save(auditLog);
		this.logUtility.logMessage("Audit event logged: " + eventType + " - " + status + " - " + requestId, INFO, log);
	}

	// CREATE Events
	public void logCreateReceived(String paymentId) {
		logInternal(null, null, paymentId, CardEventType.CREATE, CardOperationStatus.REQUEST_RECEIVED,
				CardEventSource.ISSUING, null);
	}

	public void logCreateInitiated(UUID requestId, String paymentId) {
		logInternal(requestId, null, paymentId, CardEventType.CREATE, CardOperationStatus.REQUEST_INITIATED,
				CardEventSource.VIRTUAL_CARD, null);
	}

	public void logCreateAccepted(UUID requestId, String paymentId) {
		logInternal(requestId, null, paymentId, CardEventType.CREATE, CardOperationStatus.REQUEST_ACCEPTED,
				CardEventSource.ZETA, null);
	}

	public void logCreateRejected(UUID requestId, String paymentId) {
		logInternal(requestId, null, paymentId, CardEventType.CREATE, CardOperationStatus.REQUEST_REJECTED,
				CardEventSource.ZETA, null);
	}

	public void logCreateCallbackFailure(UUID requestId, String paymentId) {
		logInternal(requestId, null, paymentId, CardEventType.CREATE, CardOperationStatus.CALLBACK_PROCESSING_FAILED,
				CardEventSource.ZETA, null);
	}

	public void logCardCreated(UUID requestId, UUID cardGuid, String paymentId) {
		logInternal(requestId, cardGuid, paymentId, CardEventType.CREATE, CardOperationStatus.CARD_ISSUED,
				CardEventSource.ZETA, null);
	}

	public void logCardCreationFailed(UUID requestId, UUID cardGuid, String paymentId) {
		logInternal(requestId, cardGuid, paymentId, CardEventType.CREATE, CardOperationStatus.CARD_ISSUE_FAILED,
				CardEventSource.ZETA, null);
	}

	// CANCEL Events
	public void logCancelReceived(String paymentId) {
		logInternal(null, null, paymentId, CardEventType.CANCEL, CardOperationStatus.REQUEST_RECEIVED,
				CardEventSource.ISSUING, null);
	}

	public void logCancelInitiated(UUID requestId, UUID cardGuid, String paymentId) {
		logInternal(requestId, cardGuid, paymentId, CardEventType.CANCEL, CardOperationStatus.REQUEST_INITIATED,
				CardEventSource.VIRTUAL_CARD, null);
	}

	public void logCancelAccepted(UUID requestId, UUID cardGuid, String paymentId) {
		logInternal(requestId, cardGuid, paymentId, CardEventType.CANCEL, CardOperationStatus.REQUEST_ACCEPTED,
				CardEventSource.ZETA, null);
	}

	public void logCancelRejected(UUID requestId, UUID cardGuid, String paymentId) {
		logInternal(requestId, cardGuid, paymentId, CardEventType.CANCEL, CardOperationStatus.REQUEST_REJECTED,
				CardEventSource.ZETA, null);
	}

	public void logCancelCallbackFailure(UUID requestId, UUID cardGuid, String paymentId) {
		logInternal(requestId, cardGuid, paymentId, CardEventType.CANCEL,
				CardOperationStatus.CALLBACK_PROCESSING_FAILED, CardEventSource.ZETA, null);
	}

	public void logCardCancelled(UUID requestId, UUID cardGuid, String paymentId) {
		logInternal(requestId, cardGuid, paymentId, CardEventType.CANCEL, CardOperationStatus.CARD_CANCELLED,
				CardEventSource.ZETA, null);
	}

	public void logCardCancellationFailed(UUID requestId, UUID cardGuid, String paymentId) {
		logInternal(requestId, cardGuid, paymentId, CardEventType.CANCEL, CardOperationStatus.CARD_CANCELLATION_FAILED,
				CardEventSource.ZETA, null);
	}

	// RESET Events
	public void logResetReceived(UUID requestId, String paymentId, String receiptId) {
		logInternal(requestId, null, paymentId, CardEventType.RESET, CardOperationStatus.REQUEST_RECEIVED,
				CardEventSource.ISSUING, receiptId);
	}

	public void logResetInitiated(UUID requestId, String paymentId, String receiptId) {
		logInternal(requestId, null, paymentId, CardEventType.RESET, CardOperationStatus.REQUEST_INITIATED,
				CardEventSource.VIRTUAL_CARD, receiptId);
	}

	public void logResetAccepted(UUID requestId, String paymentId, String receiptId) {
		logInternal(requestId, null, paymentId, CardEventType.RESET, CardOperationStatus.REQUEST_ACCEPTED,
				CardEventSource.ZETA, receiptId);
	}

	public void logResetRejected(UUID requestId, String paymentId, String receiptId) {
		logInternal(requestId, null, paymentId, CardEventType.RESET, CardOperationStatus.REQUEST_REJECTED,
				CardEventSource.ZETA, receiptId);
	}

	public void logCardReset(UUID requestId, UUID cardGuid, String paymentId, String receiptId) {
		logInternal(requestId, cardGuid, paymentId, CardEventType.RESET, CardOperationStatus.AUTH_RESET,
				CardEventSource.ZETA, receiptId);
	}

	public void logCardResetFailed(UUID requestId, UUID cardGuid, String paymentId, String receiptId) {
		logInternal(requestId, cardGuid, paymentId, CardEventType.RESET, CardOperationStatus.AUTH_RESET_FAILED,
				CardEventSource.ZETA, receiptId);
	}

	public void logResetCallbackFailure(UUID requestId, String paymentId, String receiptId) {
		logInternal(requestId, null, paymentId, CardEventType.RESET, CardOperationStatus.CALLBACK_PROCESSING_FAILED,
				CardEventSource.ZETA, receiptId);
	}

	public void logAuthAccepted(UUID cardGuid, String paymentId, String receiptId) {
		logInternal(null, cardGuid, paymentId, CardEventType.AUTH, CardOperationStatus.REQUEST_ACCEPTED,
				CardEventSource.VIRTUAL_CARD, receiptId);
	}

	public void logAuthRejected(UUID cardGuid, String paymentId, String receiptId) {
		logInternal(null, cardGuid, paymentId, CardEventType.AUTH, CardOperationStatus.REQUEST_REJECTED,
				CardEventSource.VIRTUAL_CARD, receiptId);
	}

	public void logAuthRecordFailure(UUID cardGuid, String receiptId, String errorMessage) {
		logInternal(null, cardGuid, "UNKNOWN", CardEventType.AUTH, CardOperationStatus.ERROR_OCCURRED,
				CardEventSource.VIRTUAL_CARD, receiptId);
		log.error("Auth record processing failed for cardGuid={} receiptId={} Error={}", cardGuid, receiptId,
				errorMessage);
	}

	// Clear Logging
	public void logClearRecordSuccess(UUID cardGuid, String paymentId, String receiptId) {
		logInternal(null, cardGuid, paymentId, CardEventType.CLEARING, CardOperationStatus.REQUEST_ACCEPTED,
				CardEventSource.VIRTUAL_CARD, receiptId);
	}

	public void logClearRecordFailure(UUID cardGuid, String receiptId, String errorMessage) {
		logInternal(null, cardGuid, "UNKNOWN", CardEventType.CLEARING, CardOperationStatus.ERROR_OCCURRED,
				CardEventSource.VIRTUAL_CARD, receiptId);
		log.error("Clear record processing failed for cardGuid={} receiptId={} Error={}", cardGuid, receiptId,
				errorMessage);
	}

	public void logException(UUID requestId, UUID cardGuid, String paymentId, String receiptId, CardEventType eventType,
			Exception ex) {
		logInternal(requestId, cardGuid, paymentId, eventType, CardOperationStatus.EXCEPTION_OCCURRED,
				CardEventSource.VIRTUAL_CARD, receiptId);
		log.error("Exception occurred during [{}]: {}", eventType, ex.getMessage(), ex);
	}

	public void logError(UUID requestId, String paymentId, String receiptId, CardEventType eventType,
			String errorMessage) {
		logInternal(requestId, null, paymentId, eventType, CardOperationStatus.ERROR_OCCURRED,
				CardEventSource.VIRTUAL_CARD, receiptId);
		log.error("Error occurred during [{}]: {}", eventType, errorMessage);
	}

}
