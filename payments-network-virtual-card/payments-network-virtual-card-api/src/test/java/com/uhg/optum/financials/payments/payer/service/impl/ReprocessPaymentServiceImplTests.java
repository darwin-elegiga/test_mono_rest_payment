package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.enums.CardOperationStatus;
import com.uhg.optum.financials.payments.payer.model.Address;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.exception.QueuePublishException;
import com.uhg.optum.financials.payments.payer.model.ShippingDetails;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.ReprocessLockRepository;
import com.uhg.optum.financials.payments.payer.service.LoadPaymentCardCallbackService;
import com.uhg.optum.financials.payments.payer.service.ReprocessPaymentService;
import com.uhg.optum.financials.payments.payer.service.StorageQueueProducer;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.PaymentRequestDtoUtil;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.domain.Pageable;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.CHUNK_SIZE_FOR_RE_PROCESS_PAYMENT;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_MSG_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.enums.Status.PE;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.contains;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.BDDMockito.never;
import static org.mockito.BDDMockito.times;
import static org.mockito.BDDMockito.verify;
import static org.mockito.BDDMockito.willThrow;

@ExtendWith(MockitoExtension.class)
class ReprocessPaymentServiceImplTests {

	@InjectMocks
	ReprocessPaymentServiceImpl reprocessPaymentService;

	@Mock
	StorageQueueProducer storageQueueProducer;

	@Mock
	PaymentRequestRepository paymentRequestRepository;

	@Mock
	CardCreationRepository cardCreationRepository;

	@Mock
	ReprocessLockRepository reprocessLockRepository;

	@Mock
	PaymentRequestDtoUtil paymentRequestDtoUtil;

	@Mock
	LogUtility logUtility;

	@Mock
	LoadPaymentCardCallbackService loadPaymentCardCallbackService;

	/**
	 * In production this is a {@code @Lazy} self-reference resolved through Spring's
	 * proxy, which is what makes {@code @Async} actually apply. Here it's just a mock —
	 * so tests only verify that {@code tryReprocessPaymentRequest} delegates to it for
	 * the correlation case; the actual bulk-dispatch logic is tested by calling
	 * {@code reprocessByCorrelationIdAsync} directly on the real object under test.
	 */
	@Mock
	ReprocessPaymentService self;

	private PaymentRequest mockPaymentRequest;

	@BeforeEach
	void setUp() {
		Mockito.lenient().when(this.reprocessLockRepository.tryAcquireLock(any())).thenReturn(1);

		this.mockPaymentRequest = new PaymentRequest();
		this.mockPaymentRequest.setCorrelationId("test-correlation-id");
		this.mockPaymentRequest.setOrgShortCode("ORG123");
		this.mockPaymentRequest.setAccountShortCode("ACC123");
		this.mockPaymentRequest.setSubAccountShortCode("SUB123");

		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("123");
		paymentDetails.setIssuerPaymentId("issuer123");
		paymentDetails.setReferenceId("ref123");
		paymentDetails.setPaymentDate(LocalDate.parse("2023-10-01"));
		paymentDetails.setAdjudicationCutoffDate(LocalDate.parse("2023-10-02"));
		paymentDetails.setAdjudicationVersion("v1");
		paymentDetails.setAmount(BigDecimal.valueOf(100.50));
		paymentDetails.setRemitMethod("ACH");
		paymentDetails.setJurisdictionalBody("Federal");
		paymentDetails.setNumberOfAttachedPdfs(2);
		paymentDetails.setSettlementTime("12:00");
		Address address = new Address();
		address.setLine1("123 Main St");
		address.setLine2("Apt 4B");
		address.setCity("New York");
		address.setState("NY");
		address.setCountry("USA");
		address.setPostalCode("10001");
		ShippingDetails shippingDetails = new ShippingDetails();
		shippingDetails.setName("John Doe");
		shippingDetails.setAddress(address);
		this.mockPaymentRequest.setShippingDetails(shippingDetails);
		this.mockPaymentRequest.setPaymentDetails(paymentDetails);
	}

	// ----- tryReprocessPaymentRequest: correlation branch just locks + delegates -----

	@Test
	void testTryReprocess_ByCorrelationId_AcquiresLockAndDelegatesAsync() {
		boolean result = this.reprocessPaymentService.tryReprocessPaymentRequest("test-correlation-id", null);

		Assertions.assertThat(result).isTrue();
		verify(this.reprocessLockRepository).tryAcquireLock("correlation:test-correlation-id");
		verify(this.self).reprocessByCorrelationIdAsync("test-correlation-id", "correlation:test-correlation-id");
	}

	@Test
	void testTryReprocess_ByCorrelationId_AlreadyLocked_ReturnsFalse() {
		Mockito.when(this.reprocessLockRepository.tryAcquireLock("correlation:test-correlation-id")).thenReturn(0);

		boolean result = this.reprocessPaymentService.tryReprocessPaymentRequest("test-correlation-id", null);

		Assertions.assertThat(result).isFalse();
		verify(this.self, never()).reprocessByCorrelationIdAsync(any(), any());
	}

	// ----- reprocessByCorrelationIdAsync: the actual bulk-dispatch logic -----

	@Test
	void testReprocessByCorrelationIdAsync_NoPaymentRequests() {
		Mockito
			.when(this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
					eq("test-correlation-id"), eq(PE.name()), any(Long.class), any(Pageable.class)))
			.thenReturn(List.of());

		this.reprocessPaymentService.reprocessByCorrelationIdAsync("test-correlation-id",
				"correlation:test-correlation-id");

		verify(this.storageQueueProducer, never()).sendPaymentEventToQueue(any(PaymentRequestDto.class));
		verify(this.reprocessLockRepository).releaseLock("correlation:test-correlation-id");
	}

	@Test
	void testReprocessByCorrelationIdAsync_UsesKeysetPagination_SeeksByLastIdNotOffset() {
		// A full chunk on page 1 forces a page 2 fetch. The repository must be seeked
		// using the last id actually seen on page 1 (keyset/seek pagination), not a
		// page-number/offset — offset pagination here would skip rows once dispatches
		// start flipping status out of the filtered set mid-sweep.
		List<PaymentRequest> firstPage = new ArrayList<>();
		for (long id = 1; id <= CHUNK_SIZE_FOR_RE_PROCESS_PAYMENT; id++) {
			firstPage.add(minimalPaymentRequest(id));
		}
		long lastIdOnFirstPage = CHUNK_SIZE_FOR_RE_PROCESS_PAYMENT;
		PaymentRequest secondPageRow = minimalPaymentRequest(lastIdOnFirstPage + 1);

		Mockito.when(this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
				eq("test-correlation-id"), eq(PE.name()), eq(0L), any(Pageable.class)))
			.thenReturn(firstPage);
		Mockito
			.when(this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
					eq("test-correlation-id"), eq(PE.name()), eq(lastIdOnFirstPage), any(Pageable.class)))
			.thenReturn(List.of(secondPageRow));

		this.reprocessPaymentService.reprocessByCorrelationIdAsync("test-correlation-id",
				"correlation:test-correlation-id");

		verify(this.paymentRequestRepository).findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
				eq("test-correlation-id"), eq(PE.name()), eq(lastIdOnFirstPage), any(Pageable.class));
		verify(this.storageQueueProducer, times((int) lastIdOnFirstPage + 1)).sendPaymentEventToQueue(any());
		verify(this.reprocessLockRepository).releaseLock("correlation:test-correlation-id");
	}

	private PaymentRequest minimalPaymentRequest(long id) {
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setId(id);
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId(String.valueOf(id));
		paymentRequest.setPaymentDetails(paymentDetails);
		return paymentRequest;
	}

	@Test
	void testReprocessByCorrelationIdAsync_Success() {
		PaymentRequestDto mockDto = new PaymentRequestDto();
		Mockito
			.when(this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
					eq("test-correlation-id"), eq(PE.name()), any(Long.class), any(Pageable.class)))
			.thenReturn(List.of(this.mockPaymentRequest));
		Mockito.when(this.paymentRequestDtoUtil.mapToDto(this.mockPaymentRequest)).thenReturn(mockDto);

		this.reprocessPaymentService.reprocessByCorrelationIdAsync("test-correlation-id",
				"correlation:test-correlation-id");

		verify(this.storageQueueProducer, times(1)).sendPaymentEventToQueue(eq(mockDto));
		verify(this.loadPaymentCardCallbackService, times(1)).pollAndCallback("123");
		verify(this.reprocessLockRepository).releaseLock("correlation:test-correlation-id");
	}

	@Test
	void testReprocessByCorrelationIdAsync_Exception_StillReleasesLock() {
		Mockito
			.when(this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
					eq("test-correlation-id"), eq(PE.name()), any(Long.class), any(Pageable.class)))
			.thenReturn(List.of(this.mockPaymentRequest));
		willThrow(new RuntimeException("Test Exception")).given(this.storageQueueProducer)
			.sendPaymentEventToQueue(any());

		this.reprocessPaymentService.reprocessByCorrelationIdAsync("test-correlation-id",
				"correlation:test-correlation-id");

		verify(this.logUtility, times(1)).logMessage(eq(PAYMENT_MSG_EXCEPTION_LOG), eq(ERROR), any());
		verify(this.reprocessLockRepository).releaseLock("correlation:test-correlation-id");
	}

	@Test
	void testReprocessByCorrelationIdAsync_QueuePublishException_RaisesP3Alert_DoesNotPropagate() {
		Mockito
			.when(this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
					eq("test-correlation-id"), eq(PE.name()), any(Long.class), any(Pageable.class)))
			.thenReturn(List.of(this.mockPaymentRequest));
		willThrow(new QueuePublishException("Queue unavailable", new RuntimeException("connection refused")))
			.given(this.storageQueueProducer)
			.sendPaymentEventToQueue(any());

		// This method is void/@Async — an exception can never propagate back to a
		// caller, so it must be fully handled (logged) here rather than rethrown.
		this.reprocessPaymentService.reprocessByCorrelationIdAsync("test-correlation-id",
				"correlation:test-correlation-id");

		verify(this.logUtility).logMessageWithPriority(contains("Queue unavailable"), eq(ERROR), any(), eq("P3"));
		verify(this.reprocessLockRepository).releaseLock("correlation:test-correlation-id");
	}

	@Test
	void testGetReprocessPaymentCount() {
		Mockito.when(this.paymentRequestRepository.countPaymentRequestByCorrelationIdAndStatus(any(), any()))
			.thenReturn(1);
		Assertions.assertThat(this.reprocessPaymentService.getReprocessPaymentRequestCount("123", null)).isEqualTo(1);
	}

	@Test
	void testReprocessByCorrelationIdAsync_CardIssued_ResumesCompletionPipeline_DoesNotResubmit() {
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(UUID.randomUUID());
		cardCreation.setCardGuid(UUID.randomUUID());
		cardCreation.setStatus(CardOperationStatus.CARD_ISSUED.name());

		Mockito
			.when(this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
					eq("test-correlation-id"), eq(PE.name()), any(Long.class), any(Pageable.class)))
			.thenReturn(List.of(this.mockPaymentRequest));
		Mockito.when(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc("123"))
			.thenReturn(Optional.of(cardCreation));

		this.reprocessPaymentService.reprocessByCorrelationIdAsync("test-correlation-id",
				"correlation:test-correlation-id");

		// A card already exists at Zeta — must resume the completion pipeline via a
		// reconstructed webhook event, never resubmit through the submission pipeline.
		verify(this.storageQueueProducer, never()).sendPaymentEventToQueue(any());
		verify(this.storageQueueProducer, times(1)).sendVirtualCardEventToQueue(any(ZetaWebhookDto.class));
		// Resuming the completion pipeline directly doesn't involve waiting on a fresh
		// Zeta webhook, so the polling safety net isn't needed here.
		verify(this.loadPaymentCardCallbackService, never()).pollAndCallback(any());
	}

	@Test
	void testReprocessByCorrelationIdAsync_RequestAccepted_SkipsAutomatedReprocess() {
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(UUID.randomUUID());
		cardCreation.setStatus(CardOperationStatus.REQUEST_ACCEPTED.name());

		Mockito
			.when(this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
					eq("test-correlation-id"), eq(PE.name()), any(Long.class), any(Pageable.class)))
			.thenReturn(List.of(this.mockPaymentRequest));
		Mockito.when(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc("123"))
			.thenReturn(Optional.of(cardCreation));

		this.reprocessPaymentService.reprocessByCorrelationIdAsync("test-correlation-id",
				"correlation:test-correlation-id");

		// Zeta's outcome is still in flight — must not resubmit or resume anything, and
		// the per-payment lock must be released immediately since nothing was dispatched.
		verify(this.storageQueueProducer, never()).sendPaymentEventToQueue(any());
		verify(this.storageQueueProducer, never()).sendVirtualCardEventToQueue(any());
		verify(this.reprocessLockRepository).releaseLock("payment:123");
	}

	@Test
	void testReprocessByCorrelationIdAsync_TerminallyFailed_ResubmitsThroughSubmissionPipeline() {
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(UUID.randomUUID());
		cardCreation.setStatus(CardOperationStatus.REQUEST_REJECTED.name());
		PaymentRequestDto mockDto = new PaymentRequestDto();

		Mockito
			.when(this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
					eq("test-correlation-id"), eq(PE.name()), any(Long.class), any(Pageable.class)))
			.thenReturn(List.of(this.mockPaymentRequest));
		Mockito.when(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc("123"))
			.thenReturn(Optional.of(cardCreation));
		Mockito.when(this.paymentRequestDtoUtil.mapToDto(this.mockPaymentRequest)).thenReturn(mockDto);

		this.reprocessPaymentService.reprocessByCorrelationIdAsync("test-correlation-id",
				"correlation:test-correlation-id");

		// Zeta definitively closed this attempt out — safe to restart from the top.
		verify(this.storageQueueProducer, times(1)).sendPaymentEventToQueue(eq(mockDto));
		verify(this.storageQueueProducer, never()).sendVirtualCardEventToQueue(any());
		verify(this.loadPaymentCardCallbackService, times(1)).pollAndCallback("123");
	}

	@Test
	void testReprocessByCorrelationIdAsync_CardIssueFailed_SkipsAutomatedReprocess() {
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(UUID.randomUUID());
		cardCreation.setStatus(CardOperationStatus.CARD_ISSUE_FAILED.name());

		Mockito
			.when(this.paymentRequestRepository.findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(
					eq("test-correlation-id"), eq(PE.name()), any(Long.class), any(Pageable.class)))
			.thenReturn(List.of(this.mockPaymentRequest));
		Mockito.when(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc("123"))
			.thenReturn(Optional.of(cardCreation));

		this.reprocessPaymentService.reprocessByCorrelationIdAsync("test-correlation-id",
				"correlation:test-correlation-id");

		// Zeta explicitly rejected card creation via webhook callback — deliberately
		// excluded from automated reprocessing, routed to manual review instead.
		verify(this.storageQueueProducer, never()).sendPaymentEventToQueue(any());
		verify(this.storageQueueProducer, never()).sendVirtualCardEventToQueue(any());
		verify(this.reprocessLockRepository).releaseLock("payment:123");
	}

	// ----- payment-id branch of tryReprocessPaymentRequest: fully synchronous -----

	@Test
	void testGetReprocessPaymentCount_ByPaymentId_Eligible() {
		this.mockPaymentRequest.setStatus(PE.name());
		Mockito.when(this.paymentRequestRepository.findByPaymentId("123")).thenReturn(this.mockPaymentRequest);

		Assertions.assertThat(this.reprocessPaymentService.getReprocessPaymentRequestCount(null, "123")).isEqualTo(1);
	}

	@Test
	void testGetReprocessPaymentCount_ByPaymentId_NotPE_ReturnsZero() {
		this.mockPaymentRequest.setStatus("PI");
		Mockito.when(this.paymentRequestRepository.findByPaymentId("123")).thenReturn(this.mockPaymentRequest);

		Assertions.assertThat(this.reprocessPaymentService.getReprocessPaymentRequestCount(null, "123")).isEqualTo(0);
	}

	@Test
	void testGetReprocessPaymentCount_ByPaymentId_NotFound_ReturnsZero() {
		Mockito.when(this.paymentRequestRepository.findByPaymentId("missing")).thenReturn(null);

		Assertions.assertThat(this.reprocessPaymentService.getReprocessPaymentRequestCount(null, "missing"))
			.isEqualTo(0);
	}

	@Test
	void testTryReprocess_ByPaymentId_DispatchesSinglePayment() {
		this.mockPaymentRequest.setStatus(PE.name());
		PaymentRequestDto mockDto = new PaymentRequestDto();
		Mockito.when(this.paymentRequestRepository.findByPaymentId("123")).thenReturn(this.mockPaymentRequest);
		Mockito.when(this.paymentRequestDtoUtil.mapToDto(this.mockPaymentRequest)).thenReturn(mockDto);

		boolean result = this.reprocessPaymentService.tryReprocessPaymentRequest(null, "123");

		Assertions.assertThat(result).isTrue();
		verify(this.reprocessLockRepository).tryAcquireLock("payment:123");
		verify(this.paymentRequestRepository, never()).findByCorrelationIdAndStatusAndIdGreaterThanOrderByIdAsc(any(),
				any(), any(), any());
		verify(this.storageQueueProducer, times(1)).sendPaymentEventToQueue(eq(mockDto));
		verify(this.loadPaymentCardCallbackService, times(1)).pollAndCallback("123");
	}

	@Test
	void testTryReprocess_ByPaymentId_AlreadyLocked_ReturnsFalse() {
		this.mockPaymentRequest.setStatus(PE.name());
		Mockito.when(this.paymentRequestRepository.findByPaymentId("123")).thenReturn(this.mockPaymentRequest);
		Mockito.when(this.reprocessLockRepository.tryAcquireLock("payment:123")).thenReturn(0);

		boolean result = this.reprocessPaymentService.tryReprocessPaymentRequest(null, "123");

		Assertions.assertThat(result).isFalse();
		verify(this.storageQueueProducer, never()).sendPaymentEventToQueue(any());
		verify(this.storageQueueProducer, never()).sendVirtualCardEventToQueue(any());
	}

	@Test
	void testTryReprocess_ByPaymentId_NotEligible_ReturnsFalseWithoutLocking() {
		this.mockPaymentRequest.setStatus("PI");
		Mockito.when(this.paymentRequestRepository.findByPaymentId("123")).thenReturn(this.mockPaymentRequest);

		boolean result = this.reprocessPaymentService.tryReprocessPaymentRequest(null, "123");

		Assertions.assertThat(result).isFalse();
		verify(this.reprocessLockRepository, never()).tryAcquireLock(any());
		verify(this.storageQueueProducer, never()).sendPaymentEventToQueue(any());
		verify(this.storageQueueProducer, never()).sendVirtualCardEventToQueue(any());
	}

}
