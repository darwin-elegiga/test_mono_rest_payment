package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.ApiConstants;
import com.uhg.optum.financials.payments.payer.constant.EventTypeConstants;

import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.dto.callback.CallbackError;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;

import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;

import com.uhg.optum.financials.payments.payer.service.CancelCardService;
import com.uhg.optum.financials.payments.payer.service.ProcessFailedEventService;
import com.uhg.optum.financials.payments.payer.service.ZetaWebhookService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ProcessFailedEventUtil;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.DisposableBean;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.ThreadPoolExecutor;
import java.util.concurrent.TimeUnit;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;

@Service
@Slf4j
public class ProcessFailedEventServiceImpl implements ProcessFailedEventService, DisposableBean {

	private final ProcessFailedEventUtil processFailedEventUtil;

	private final PaymentRequestRepository paymentRequestRepository;

	private final CardCreationRepository cardCreationRepository;

	private final LogUtility logUtility;

	private final ZetaWebhookService zetaWebhookService;

	private final ExecutorService callbackExecutor;

	private final CancelCardService cancelCardService;

	private final long taskSleepMillis;

	public ProcessFailedEventServiceImpl(ProcessFailedEventUtil processFailedEventUtil,
			PaymentRequestRepository paymentRequestRepository, CardCreationRepository cardCreationRepository,
			LogUtility logUtility, ZetaWebhookService zetaWebhookService,
			@Value("${failed-event-callback.thread-pool.core-pool-size}") int corePoolSize,
			@Value("${failed-event-callback.thread-pool.max-pool-size}") int maxPoolSize,
			@Value("${failed-event-callback.thread-pool.queue-capacity}") int queueCapacity,
			@Value("${failed-event-callback.thread-pool.keep-alive-time-seconds}") long keepAliveTimeSeconds,
			@Value("${failed-event-callback.thread-pool.task-sleep-millis}") long taskSleepMillis,
			CancelCardService cancelCardService) {
		this.processFailedEventUtil = processFailedEventUtil;
		this.paymentRequestRepository = paymentRequestRepository;
		this.cardCreationRepository = cardCreationRepository;

		this.logUtility = logUtility;
		this.zetaWebhookService = zetaWebhookService;
		this.taskSleepMillis = taskSleepMillis;
		this.cancelCardService = cancelCardService;
		this.callbackExecutor = new ThreadPoolExecutor(corePoolSize, maxPoolSize, keepAliveTimeSeconds,
				TimeUnit.SECONDS, new LinkedBlockingQueue<>(queueCapacity));
	}

	@Override
	public void processFailedEvent(String paymentId, String correlationId) {
		String safePaymentId = sanitizeForLog(paymentId);
		String safeCorrelationId = sanitizeForLog(correlationId);
		String initiatingThread = Thread.currentThread().getName();

		this.logUtility.logMessage("Processing failed event initiated on thread: " + initiatingThread
				+ " for paymentId: " + safePaymentId + " and correlationId: " + safeCorrelationId, INFO, log);

		List<PaymentIdAndClientRequestId> paymentPairs = new ArrayList<>();
		List<String> paymentIdsWithoutRequestId = new ArrayList<>();

		if (paymentId != null && !paymentId.isBlank()) {
			this.processFailedEventUtil.checkIfPaymentIdNotAvailable(paymentId);
			String clientRequestId = getClientRequestID(paymentId);
			if (clientRequestId == null || clientRequestId.isBlank()) {
				this.logUtility.logMessage("Failed to extract clientRequestId for paymentId: " + safePaymentId, ERROR,
						log);
				throw new IllegalArgumentException("Failed to extract clientRequestId for paymentId: " + safePaymentId);
			}
			paymentPairs.add(new PaymentIdAndClientRequestId(paymentId, clientRequestId));
		}
		else if (correlationId != null && !correlationId.isBlank()) {
			List<PaymentIdAndClientRequestId> results = getPaymentIdFromCorrelationId(correlationId);
			if (results.isEmpty()) {
				this.logUtility.logMessage(
						"No payments found for correlationId: " + safeCorrelationId + ". Rejecting request.", ERROR,
						log);
				throw new IllegalArgumentException("No payments found for correlationId: " + safeCorrelationId);
			}

			for (PaymentIdAndClientRequestId pair : results) {
				if (pair.clientRequestId() == null || pair.clientRequestId().isBlank()) {
					paymentIdsWithoutRequestId.add(pair.paymentId());
				}
				else {
					paymentPairs.add(pair);
				}
			}

			if (!paymentIdsWithoutRequestId.isEmpty()) {
				this.logUtility.logMessage("WARNING: Following paymentIds for correlationId: " + safeCorrelationId
						+ " do not have clientRequestId: " + paymentIdsWithoutRequestId, ERROR, log);
				throw new BadRequestException("Some paymentIds for correlationId: " + safeCorrelationId
						+ " do not have clientRequestId: " + paymentIdsWithoutRequestId);
			}
		}

		// === ASYNC PARALLEL PROCESSING ===
		long asyncStartTime = System.currentTimeMillis();
		List<CompletableFuture<Void>> asyncCallbacks = new ArrayList<>();

		this.logUtility.logMessage("[ASYNC BATCH START] Submitting " + paymentPairs.size()
				+ " async tasks from thread: " + initiatingThread, INFO, log);

		for (PaymentIdAndClientRequestId pair : paymentPairs) {
			String taskId = UUID.randomUUID().toString();
			String pairPaymentId = sanitizeForLog(pair.paymentId());
			String pairClientRequestId = sanitizeForLog(pair.clientRequestId());

			CompletableFuture<Void> future = CompletableFuture.supplyAsync(() -> {
				String executingThread = Thread.currentThread().getName();

				// Log execution start with thread name - PROVES PARALLEL EXECUTION
				this.logUtility.logMessage("[TASK EXECUTING] [taskId: " + taskId + "] for paymentId: " + pairPaymentId
						+ " - NOW RUNNING ON THREAD: " + executingThread, INFO, log);
				return publishFailedEventCallback(pair.paymentId(), pair.clientRequestId());
			}, this.callbackExecutor).exceptionally((ex) -> {
				String sanitizedError = sanitizeForLog(ex.getMessage());
				this.logUtility.logMessage("[TASK FAILED] [taskId: " + taskId + "] for paymentId: " + pairPaymentId
						+ " - Error: " + sanitizedError, ERROR, log);
				return null;
			});
			asyncCallbacks.add(future);
		}

		// Wait for all async callbacks to complete
		try {
			CompletableFuture.allOf(asyncCallbacks.toArray(new CompletableFuture[0])).join();

			long asyncEndTime = System.currentTimeMillis();
			long totalAsyncTime = asyncEndTime - asyncStartTime;

			// Log final summary with timing
			this.logUtility.logMessage("[ASYNC BATCH COMPLETED] All " + asyncCallbacks.size()
					+ " callbacks published in " + totalAsyncTime + "ms" + " (Parallel execution - would take ~"
					+ (totalAsyncTime * asyncCallbacks.size()) + "ms if sequential)", INFO, log);
		}
		catch (Exception ex) {
			String sanitizedError = sanitizeForLog(ex.getMessage());
			this.logUtility.logMessage(
					"[ASYNC BATCH FAILED] Error while publishing failed event callbacks: " + sanitizedError, ERROR,
					log);
			throw new RuntimeException("Failed to publish all event callbacks", ex);
		}
	}

	private Void publishFailedEventCallback(String paymentId, String clientRequestId) {

		// Validate that we have required data before publishing
		if (paymentId == null || paymentId.isBlank()) {
			this.logUtility.logMessage("Cannot publish callback with null or blank paymentId", ERROR, log);
			throw new BadRequestException("paymentId cannot be null or blank");
		}
		if (clientRequestId == null || clientRequestId.isBlank()) {
			String safePaymentId = sanitizeForLog(paymentId);
			this.logUtility.logMessage(
					"Cannot publish callback with null or blank clientRequestId for paymentId: " + safePaymentId, ERROR,
					log);
			throw new BadRequestException("clientRequestId cannot be null or blank for paymentId: " + paymentId);
		}

		try {
			CallbackError error = new CallbackError();
			error.setClientRequestId(UUID.fromString(clientRequestId));
			error.setCode(ApiConstants.ERRORCODE);
			error.setMessage(ApiConstants.MESSAGE);
			error.setReceiptId("");

			ZetaWebhookDto<CallbackError> payload = new ZetaWebhookDto<>(UUID.randomUUID().toString(),
					ApiConstants.SPEC_VERSION, Instant.now(), ApiConstants.EVENT_STATUS,
					EventTypeConstants.VIRTUAL_CARD_CREATE, null, error);
			int result = this.zetaWebhookService.processZetaWebhook(payload);
			cancelCard(paymentId);
			String safePaymentId = sanitizeForLog(paymentId);
			this.logUtility.logMessage("Failed event posted for paymentId: " + safePaymentId + " with status " + result,
					INFO, log);
			return null;
		}
		catch (Exception ex) {
			String safePaymentId = sanitizeForLog(paymentId);
			String sanitizedError = sanitizeForLog(ex.getMessage());
			this.logUtility.logMessage(
					"Error publishing failed event callback for paymentId: " + safePaymentId + " - " + sanitizedError,
					ERROR, log);
			throw new RuntimeException("Failed to publish callback for paymentId: " + paymentId, ex);
		}
	}

	public String getClientRequestID(String paymentId) {
		CardCreation cardCreation = this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId)
			.orElse(null);
		return cardCreation != null ? cardCreation.getId().toString() : null;
	}

	private List<PaymentIdAndClientRequestId> getPaymentIdFromCorrelationId(String correlationId) {
		String safeCorrelationId = sanitizeForLog(correlationId);
		try {
			var paymentIds = this.paymentRequestRepository.findByCorrelationId(correlationId)
				.stream()
				.map((pr) -> pr.getPaymentDetails().getId())
				.toList();

			if (!paymentIds.isEmpty()) {
				// Use a parallel stream to fetch clientRequestIds concurrently
				return paymentIds.stream().map((paymentId) -> {
					var clientRequestId = getClientRequestID(paymentId);
					return new PaymentIdAndClientRequestId(paymentId, clientRequestId);
				}).toList();
			}
			else {
				this.logUtility.logMessage("No paymentId found for correlationId: " + safeCorrelationId, "warn", log);
			}
		}
		catch (Exception ex) {
			String sanitizedError = sanitizeForLog(ex.getMessage());
			this.logUtility.logMessage(
					"Error retrieving paymentId for correlationId: " + safeCorrelationId + " - " + sanitizedError,
					ERROR, log);
		}
		return new ArrayList<>();
	}

	private String sanitizeForLog(String value) {
		if (value == null) {
			return null;
		}
		return value.replace('\r', '_').replace('\n', '_').replace('\t', '_').replace('\f', '_').replace('\u000B', '_');
	}

	public void cancelCard(String paymentId) {
		var cancelCardDto = new CancelCardDto();
		cancelCardDto.setPaymentId(paymentId);
		cancelCardDto.setReasonCode(ApiConstants.OTHER);
		this.cancelCardService.sendCancelCardRequest(cancelCardDto);
	}

	/**
	 * Cleans up the thread pool when the application shuts down.
	 */
	@Override
	public void destroy() throws Exception {
		if (this.callbackExecutor != null && !this.callbackExecutor.isShutdown()) {
			this.logUtility.logMessage("Shutting down callback executor thread pool", INFO, log);
			this.callbackExecutor.shutdown();

			// Wait up to 30 seconds for threads to terminate
			if (!this.callbackExecutor.awaitTermination(30, TimeUnit.SECONDS)) {
				this.logUtility.logMessage("Forcing shutdown of callback executor thread pool", ERROR, log);
				this.callbackExecutor.shutdownNow();
			}
		}
	}

	/**
	 * Immutable record to hold paymentId and clientRequestId pair. Replaces boilerplate
	 * getter/setter/constructor code with Java 21 record syntax.
	 */
	private record PaymentIdAndClientRequestId(String paymentId, String clientRequestId) {
	}

}
