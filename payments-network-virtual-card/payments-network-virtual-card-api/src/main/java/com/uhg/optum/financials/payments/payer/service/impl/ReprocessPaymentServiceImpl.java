package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.EventTypeConstants;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.enums.CardOperationStatus;
import com.uhg.optum.financials.payments.payer.exception.QueuePublishException;
import com.uhg.optum.financials.payments.payer.exception.ServiceUnavailableException;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.ReprocessLockRepository;
import com.uhg.optum.financials.payments.payer.service.LoadPaymentCardCallbackService;
import com.uhg.optum.financials.payments.payer.service.ReprocessPaymentService;
import com.uhg.optum.financials.payments.payer.service.StorageQueueProducer;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.PaymentRequestDtoUtil;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Lazy;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.CHUNK_SIZE_FOR_RE_PROCESS_PAYMENT;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.PAYMENT_LOAD_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_MSG_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;
import static com.uhg.optum.financials.payments.payer.enums.Status.PE;

@Service
@Slf4j
public class ReprocessPaymentServiceImpl implements ReprocessPaymentService {

	/**
	 * Must match {@code WorkerHandler.SUCCESS_EVENT} in the worker module — the two
	 * modules don't share this constant directly since they're separately deployed.
	 */
	private static final String SUCCESS_EVENT = "SUCCESS";

	private static final String CORRELATION_LOCK_PREFIX = "correlation:";

	private static final String PAYMENT_LOCK_PREFIX = "payment:";

	private final PaymentRequestRepository paymentRequestRepository;

	private final CardCreationRepository cardCreationRepository;

	private final ReprocessLockRepository reprocessLockRepository;

	private final StorageQueueProducer storageQueueProducer;

	private final LogUtility logUtility;

	private final PaymentRequestDtoUtil paymentRequestDtoUtil;

	private final LoadPaymentCardCallbackService loadPaymentCardCallbackService;

	/**
	 * Self-reference so {@link #reprocessByCorrelationIdAsync} is invoked through
	 * Spring's proxy (required for {@code @Async} to actually apply) rather than via a
	 * plain {@code this.} call, which bypasses proxying entirely.
	 */
	private final ReprocessPaymentService self;

	public ReprocessPaymentServiceImpl(PaymentRequestRepository paymentRequestRepository,
			CardCreationRepository cardCreationRepository, ReprocessLockRepository reprocessLockRepository,
			StorageQueueProducer storageQueueProducer, LogUtility logUtility,
			PaymentRequestDtoUtil paymentRequestDtoUtil, LoadPaymentCardCallbackService loadPaymentCardCallbackService,
			@Lazy ReprocessPaymentService self) {
		this.paymentRequestRepository = paymentRequestRepository;
		this.cardCreationRepository = cardCreationRepository;
		this.reprocessLockRepository = reprocessLockRepository;
		this.storageQueueProducer = storageQueueProducer;
		this.logUtility = logUtility;
		this.paymentRequestDtoUtil = paymentRequestDtoUtil;
		this.loadPaymentCardCallbackService = loadPaymentCardCallbackService;
		this.self = self;
	}

	@Override
	public int getReprocessPaymentRequestCount(String correlationId, String paymentId) {
		if (correlationId != null) {
			return this.paymentRequestRepository.countPaymentRequestByCorrelationIdAndStatus(correlationId, PE.name());
		}
		PaymentRequest paymentRequest = this.paymentRequestRepository.findByPaymentId(paymentId);
		return (paymentRequest != null && PE.name().equals(paymentRequest.getStatus())) ? 1 : 0;
	}

	@Override
	public boolean tryReprocessPaymentRequest(String correlationId, String paymentId) {
		if (correlationId != null) {
			String lockKey = CORRELATION_LOCK_PREFIX + correlationId;
			if (this.reprocessLockRepository.tryAcquireLock(lockKey) == 0) {
				this.logUtility.logMessage(
						"Correlation " + correlationId + " already has a reprocess in progress — rejecting trigger",
						INFO, log);
				return false;
			}
			this.self.reprocessByCorrelationIdAsync(correlationId, lockKey);
			return true;
		}

		PaymentRequest paymentRequest = this.paymentRequestRepository.findByPaymentId(paymentId);
		if (paymentRequest == null || !PE.name().equals(paymentRequest.getStatus())) {
			return false;
		}
		try {
			return dispatchReprocess(paymentRequest);
		}
		catch (QueuePublishException ex) {
			this.logUtility.logMessageWithPriority(PAYMENT_MSG_EXCEPTION_LOG + ex.getMessage(), ERROR, log, P3);
			throw new ServiceUnavailableException(PAYMENT_LOAD_EXCEPTION_LOG + ex.getMessage(), ex);
		}
		catch (Exception ex) {
			this.logUtility.logMessage(PAYMENT_MSG_EXCEPTION_LOG, ERROR, log);
			return false;
		}
	}

	@Async
	@Override
	public void reprocessByCorrelationIdAsync(String correlationId, String lockKey) {
		try {
			// Keyset pagination, not PageRequest.of(pageNumber, ...): dispatches in this
			// loop cause the worker to asynchronously flip rows out of status=PE while
			// the
			// sweep is still running, so an offset-based page N+1 would be computed
			// against a shrunk filtered set and skip rows. Seeking by id > lastSeenId is
			// unaffected by that.
			Pageable pageable = PageRequest.of(0, CHUNK_SIZE_FOR_RE_PROCESS_PAYMENT);
			Long lastSeenId = 0L;
			List<PaymentRequest> batch;
			do {
				batch = this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
						correlationId, PE.name(), lastSeenId, pageable);
				batch.forEach(this::dispatchReprocess);
				if (!batch.isEmpty()) {
					lastSeenId = batch.get(batch.size() - 1).getId();
				}
			}
			while (batch.size() == CHUNK_SIZE_FOR_RE_PROCESS_PAYMENT);
		}
		catch (QueuePublishException ex) {
			this.logUtility.logMessageWithPriority(PAYMENT_MSG_EXCEPTION_LOG + ex.getMessage(), ERROR, log, P3);
		}
		catch (Exception ex) {
			this.logUtility.logMessage(PAYMENT_MSG_EXCEPTION_LOG, ERROR, log);
		}
		finally {
			// This lock's only job is "don't let two bulk sweeps for the same
			// correlation overlap" — release it once every page has been dispatched,
			// regardless of outcome, so a later trigger isn't locked out forever.
			this.reprocessLockRepository.releaseLock(lockKey);
		}
	}

	/**
	 * Routes a failed payment to the right resume point instead of always replaying the
	 * original submission: a card that's already in flight or already issued at Zeta must
	 * never be resubmitted through the submission pipeline, since that would call Zeta's
	 * create-card API a second time and risk a duplicate card.
	 *
	 * <p>
	 * Also owns the per-payment lock's full lifecycle: acquires it first (shared by both
	 * the single-payment and bulk-correlation callers, so there's exactly one acquisition
	 * point), releases it immediately if nothing was actually dispatched (skip branch, or
	 * a publish failure), and otherwise leaves it held for the worker to release once it
	 * starts processing the dispatched message.
	 * @return {@code true} if the lock was acquired and a dispatch decision was made
	 * (including an intentional skip), {@code false} if a reprocess was already in
	 * progress for this payment.
	 */
	private boolean dispatchReprocess(PaymentRequest paymentRequest) {
		String paymentId = paymentRequest.getPaymentDetails().getId();
		String lockKey = PAYMENT_LOCK_PREFIX + paymentId;
		if (this.reprocessLockRepository.tryAcquireLock(lockKey) == 0) {
			this.logUtility.logMessage("Payment " + paymentId + " already has a reprocess in progress — skipping", INFO,
					log);
			return false;
		}

		try {
			Optional<CardCreation> latest = this.cardCreationRepository
				.findTopByPaymentIdOrderByCreatedAtDesc(paymentId);

			if (latest.isEmpty() || isTerminallyFailed(latest.get())) {
				this.storageQueueProducer.sendPaymentEventToQueue(this.paymentRequestDtoUtil.mapToDto(paymentRequest));
				// Mirrors the original submission flow (PaymentServiceImpl.loadPayment) —
				// without this, a reprocessed payment whose Zeta webhook also gets lost
				// has no safety net to recover it.
				this.loadPaymentCardCallbackService.pollAndCallback(paymentId);
				// Lock stays held — the worker releases it once it starts processing
				// this message.
			}
			else if (CardOperationStatus.CARD_ISSUED.name().equals(latest.get().getStatus())) {
				this.storageQueueProducer.sendVirtualCardEventToQueue(reconstructWebhookEvent(latest.get()));
				// Lock stays held — released by the worker when it starts resuming.
			}
			else {
				// REQUEST_ACCEPTED/REQUEST_INITIATED — Zeta's outcome is still in flight
				// or ambiguous; resubmitting here could race a real webhook or duplicate
				// an already-accepted request. Let the existing webhook-timeout alerting
				// handle anything genuinely stuck instead of auto-retrying it.
				// CARD_ISSUE_FAILED — Zeta explicitly rejected this card-creation request
				// via webhook callback. Although no card exists and a fresh attempt would
				// be technically safe, this is deliberately excluded from automated
				// reprocessing and routed to manual review instead.
				this.logUtility.logMessage(
						"Payment " + paymentId + " is " + latest.get().getStatus() + " — skipping automated reprocess",
						INFO, log);
				// Nothing was dispatched — no worker message exists to release this, so
				// it must happen here.
				this.reprocessLockRepository.releaseLock(lockKey);
			}
			return true;
		}
		catch (RuntimeException ex) {
			// The publish itself failed — nothing was actually sent, so nothing will
			// ever release this lock otherwise.
			this.reprocessLockRepository.releaseLock(lockKey);
			throw ex;
		}
	}

	private boolean isTerminallyFailed(CardCreation cardCreation) {
		return CardOperationStatus.REQUEST_REJECTED.name().equals(cardCreation.getStatus());
	}

	/**
	 * Rebuilds the webhook event needed to resume the completion pipeline (virtual card
	 * creation / document generation / STP / completion webhook) from durably persisted
	 * {@link CardCreation} fields, rather than requiring the original live Zeta webhook
	 * payload.
	 *
	 * <p>
	 * {@code amount} and {@code spendRules} are reconstructed from the same data sent to
	 * Zeta on the original request — {@code CardCreation.amount} is the identical cents
	 * value, and {@code CardCreation.spendRule} is the exact {@code SpendRules} entity
	 * used to build the original request, mirroring
	 * {@code VirtualCardCreateServiceImpl.buildCardCreationRequest}. Currency is not a
	 * real per-payment concept in this system — nothing upstream ever sends or stores
	 * one, every card is USD.
	 */
	private ZetaWebhookDto<CreateCardData> reconstructWebhookEvent(CardCreation cardCreation) {
		CreateCardData createCardData = new CreateCardData();
		createCardData.setClientRequestId(cardCreation.getId());
		createCardData.setCardGuid(cardCreation.getCardGuid());
		createCardData.setSingleSpend(cardCreation.getIsSingleSpend());

		CreateCardData.InitialBalance initialBalance = new CreateCardData.InitialBalance();
		initialBalance.setAmount(cardCreation.getAmount());
		initialBalance.setCurrency("USD");
		createCardData.setInitialBalance(initialBalance);

		createCardData
			.setSpendRules((cardCreation.getSpendRule() != null && cardCreation.getSpendRule().getRuleCode() != null)
					? cardCreation.getSpendRule().getRuleCode() : new ArrayList<>());

		// A fresh id is required — WorkerHandler dedupes processed webhooks by
		// (id, type), and reusing the original webhook's id would cause this resume
		// attempt to be silently dropped as a duplicate.
		return new ZetaWebhookDto<>(UUID.randomUUID().toString(), "1.0", Instant.now(), SUCCESS_EVENT,
				EventTypeConstants.VIRTUAL_CARD_CREATE, createCardData, null);
	}

}
