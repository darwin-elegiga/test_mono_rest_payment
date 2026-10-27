package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.model.AuditLog;
import com.uhg.optum.financials.payments.payer.repository.AuditLogRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.contains;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
public class AuditLoggerTests {

	@Mock
	private AuditLogRepository auditLogRepository;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private AuditLogger auditLogger;

	private final UUID requestId = UUID.randomUUID();

	private final UUID cardGuid = UUID.randomUUID();

	private final String paymentId = "payment123";

	private final String receiptId = "receipt456";

	@BeforeEach
	void setUp() {
		// No-op, handled by @InjectMocks
	}

	@Test
	void testLogCreateReceived() {
		this.auditLogger.logCreateReceived(this.paymentId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogCardCreated() {
		this.auditLogger.logCardCreated(this.requestId, this.cardGuid, this.paymentId);

		ArgumentCaptor<AuditLog> captor = ArgumentCaptor.forClass(AuditLog.class);
		verify(this.auditLogRepository).save(captor.capture());
		AuditLog log = captor.getValue();
		assertThat(log.getRequestId()).isEqualTo(this.requestId);
		assertThat(log.getCardGuid()).isEqualTo(this.cardGuid);
		assertThat(log.getPaymentId()).isEqualTo(this.paymentId);
		assertThat(log.getEventType()).isEqualTo(CardEventType.CREATE.name());
	}

	@Test
	void testLogResetReceived() {
		this.auditLogger.logResetReceived(this.requestId, this.paymentId, this.receiptId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogException() {
		Exception ex = new RuntimeException("Test exception");
		this.auditLogger.logException(this.requestId, null, this.paymentId, null, CardEventType.CREATE, ex);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogCreateInitiated() {
		this.auditLogger.logCreateInitiated(UUID.randomUUID(), this.paymentId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogCreateAccepted() {
		this.auditLogger.logCreateAccepted(UUID.randomUUID(), this.paymentId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogCreateRejected() {
		this.auditLogger.logCreateRejected(UUID.randomUUID(), this.paymentId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogCreateCallbackFailure() {
		this.auditLogger.logCreateCallbackFailure(UUID.randomUUID(), this.paymentId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogCancelInitiated() {
		this.auditLogger.logCancelInitiated(this.requestId, this.cardGuid, this.paymentId);

		ArgumentCaptor<AuditLog> captor = ArgumentCaptor.forClass(AuditLog.class);
		verify(this.auditLogRepository).save(captor.capture());
		AuditLog log = captor.getValue();
		assertThat(log.getRequestId()).isEqualTo(this.requestId);
		assertThat(log.getCardGuid()).isEqualTo(this.cardGuid);
		assertThat(log.getPaymentId()).isEqualTo(this.paymentId);
		assertThat(log.getEventType()).isEqualTo(CardEventType.CANCEL.name());
	}

	@Test
	void testLogCardCreationFailed() {
		this.auditLogger.logCancelInitiated(this.requestId, this.cardGuid, this.paymentId);

		ArgumentCaptor<AuditLog> captor = ArgumentCaptor.forClass(AuditLog.class);
		verify(this.auditLogRepository).save(captor.capture());
		AuditLog log = captor.getValue();
		assertThat(log.getRequestId()).isEqualTo(this.requestId);
		assertThat(log.getCardGuid()).isEqualTo(this.cardGuid);
		assertThat(log.getPaymentId()).isEqualTo(this.paymentId);
		assertThat(log.getEventType()).isEqualTo(CardEventType.CANCEL.name());
	}

	@Test
	void testLogCancelAccepted() {
		this.auditLogger.logCancelAccepted(this.requestId, this.cardGuid, this.paymentId);

		ArgumentCaptor<AuditLog> captor = ArgumentCaptor.forClass(AuditLog.class);
		verify(this.auditLogRepository).save(captor.capture());
		AuditLog log = captor.getValue();
		assertThat(log.getRequestId()).isEqualTo(this.requestId);
		assertThat(log.getCardGuid()).isEqualTo(this.cardGuid);
		assertThat(log.getPaymentId()).isEqualTo(this.paymentId);
		assertThat(log.getEventType()).isEqualTo(CardEventType.CANCEL.name());
	}

	@Test
	void testLogCancelRejected() {
		this.auditLogger.logCancelRejected(this.requestId, this.cardGuid, this.paymentId);

		ArgumentCaptor<AuditLog> captor = ArgumentCaptor.forClass(AuditLog.class);
		verify(this.auditLogRepository).save(captor.capture());
		AuditLog log = captor.getValue();
		assertThat(log.getRequestId()).isEqualTo(this.requestId);
		assertThat(log.getCardGuid()).isEqualTo(this.cardGuid);
		assertThat(log.getPaymentId()).isEqualTo(this.paymentId);
		assertThat(log.getEventType()).isEqualTo(CardEventType.CANCEL.name());
	}

	@Test
	void testLogCancelCallbackFailure() {
		this.auditLogger.logCancelCallbackFailure(this.requestId, this.cardGuid, this.paymentId);

		ArgumentCaptor<AuditLog> captor = ArgumentCaptor.forClass(AuditLog.class);
		verify(this.auditLogRepository).save(captor.capture());
		AuditLog log = captor.getValue();
		assertThat(log.getRequestId()).isEqualTo(this.requestId);
		assertThat(log.getCardGuid()).isEqualTo(this.cardGuid);
		assertThat(log.getPaymentId()).isEqualTo(this.paymentId);
		assertThat(log.getEventType()).isEqualTo(CardEventType.CANCEL.name());
	}

	@Test
	void testLogCardCancelled() {
		this.auditLogger.logCardCancelled(this.requestId, this.cardGuid, this.paymentId);

		ArgumentCaptor<AuditLog> captor = ArgumentCaptor.forClass(AuditLog.class);
		verify(this.auditLogRepository).save(captor.capture());
		AuditLog log = captor.getValue();
		assertThat(log.getRequestId()).isEqualTo(this.requestId);
		assertThat(log.getCardGuid()).isEqualTo(this.cardGuid);
		assertThat(log.getPaymentId()).isEqualTo(this.paymentId);
		assertThat(log.getEventType()).isEqualTo(CardEventType.CANCEL.name());
	}

	@Test
	void testLogCardCancellationFailed() {
		this.auditLogger.logCardCancellationFailed(this.requestId, this.cardGuid, this.paymentId);

		ArgumentCaptor<AuditLog> captor = ArgumentCaptor.forClass(AuditLog.class);
		verify(this.auditLogRepository).save(captor.capture());
		AuditLog log = captor.getValue();
		assertThat(log.getRequestId()).isEqualTo(this.requestId);
		assertThat(log.getCardGuid()).isEqualTo(this.cardGuid);
		assertThat(log.getPaymentId()).isEqualTo(this.paymentId);
		assertThat(log.getEventType()).isEqualTo(CardEventType.CANCEL.name());
	}

	@Test
	void testLogCancelReceived() {
		this.auditLogger.logCancelReceived(this.paymentId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogResetInitiated() {
		this.auditLogger.logResetInitiated(this.requestId, this.paymentId, this.receiptId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogResetAccepted() {
		this.auditLogger.logResetAccepted(this.requestId, this.paymentId, this.receiptId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogCardReset() {
		this.auditLogger.logCardReset(this.requestId, this.cardGuid, this.paymentId, this.receiptId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogCardResetFailed() {
		this.auditLogger.logCardResetFailed(this.requestId, null, this.paymentId, this.receiptId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogAuthReceived() {
		this.auditLogger.logAuthAccepted(this.requestId, this.paymentId, this.receiptId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

	@Test
	void testLogCardCreationFailedCase() {
		this.auditLogger.logCardCreationFailed(this.requestId, this.cardGuid, this.paymentId);

		verify(this.auditLogRepository, times(1)).save(any(AuditLog.class));
		verify(this.logUtility, times(1)).logMessage(contains("Audit event logged"), any(), any());
	}

}
