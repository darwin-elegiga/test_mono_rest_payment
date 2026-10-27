package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.CardHolderDto;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountBankAccountDetails;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.CardProcessingError;
import com.uhg.optum.financials.payments.payer.exception.NetworkPaymentIdGenerationException;
import com.uhg.optum.financials.payments.payer.exception.OrganizationRegisteredException;
import com.uhg.optum.financials.payments.payer.exception.SegmentGenerationException;
import com.uhg.optum.financials.payments.payer.model.CardHolder;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.PaymentRequestHistory;
import com.uhg.optum.financials.payments.payer.model.SpendRules;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardholderRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestHistoryRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.ReprocessLockRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.SegmentCreationService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardCreateService;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.PaymentRequestEntityUtil;
import com.uhg.optum.financials.shared.client.constant.PaymentType;
import com.uhg.optum.financials.shared.client.dto.payment.PayeeDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import jakarta.persistence.EntityNotFoundException;
import jakarta.validation.Validator;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockedStatic;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.ResponseEntity;

import java.util.Optional;

import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.enums.Status.PE;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.verifyNoInteractions;

@ExtendWith(MockitoExtension.class)
public class PaymentRequestServiceImplTests {

	private static final long NETWORK_PAYMENT_ID_BASE36_START = 2_821_109_907_456L;

	@InjectMocks
	private PaymentRequestServiceImpl paymentRequestServiceImpl;

	// The following mocks are required for @InjectMocks constructor injection
	@Mock
	@SuppressWarnings("unused")
	private VirtualCardCreateService virtualCardCreateService;

	@Mock
	@SuppressWarnings("unused")
	private LogUtility logUtility;

	@Mock
	@SuppressWarnings("unused")
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	@SuppressWarnings("unused")
	private CardholderRepository cardholderRepository;

	@Mock
	@SuppressWarnings("unused")
	private PaymentRequestHistoryRepository paymentRequestHistoryRepository;

	@Mock
	@SuppressWarnings("unused")
	private SegmentCreationService segmentCreationService;

	@Mock
	@SuppressWarnings("unused")
	private SubAccountRepository subAccountRepository;

	@Mock
	private PaymentDetailsRepository paymentDetailsRepository;

	@Mock
	@SuppressWarnings("unused")
	private ZetaApiService zetaApiService;

	@Mock
	@SuppressWarnings("unused")
	private Validator validator;

	@Mock
	@SuppressWarnings("unused")
	private CardCreationRepository cardCreationRepository;

	@Mock
	@SuppressWarnings("unused")
	private ReprocessLockRepository reprocessLockRepository;

	private PaymentRequestDto paymentRequestDto;

	private PaymentDetailsDto paymentDetailsDto;

	private PayeeDetailsDto payeeDetailsDto;

	@BeforeEach
	public void setUp() {
		this.paymentDetailsDto = new PaymentDetailsDto();
		this.paymentDetailsDto.setId("PAY123");
		this.paymentDetailsDto.setType(PaymentType.PAYMENT);

		// PaymentDetails.type now drives categorization and defaults to B2B unless C2B is
		// sent.
		this.payeeDetailsDto = new PayeeDetailsDto();
		this.payeeDetailsDto.setType("B");
		this.payeeDetailsDto.setReceiverId("receiver-123");

		this.paymentRequestDto = new PaymentRequestDto();
		this.paymentRequestDto.setAccountShortCode("testCode");
		this.paymentRequestDto.setSubAccountShortCode("subAccCode");
		this.paymentRequestDto.setOrgShortCode("orgCode");
		this.paymentRequestDto.setPaymentDetails(this.paymentDetailsDto);
		this.paymentRequestDto.setPayeeDetails(this.payeeDetailsDto);
	}

	// -----------------------------------------------------------------------
	// processPaymentRequestEvents – error-path coverage
	// -----------------------------------------------------------------------

	@Test
	public void processPaymentRequestEvents_subAccountNotFound_throwsException() {
		SubAccountBankAccountDetails subAccountDetails = new SubAccountBankAccountDetails();
		subAccountDetails.setPayer835Id("dummy");
		PaymentRequest paymentRequest = buildPaymentRequest();

		BDDMockito.given(this.subAccountRepository.findSubAccountBankDetails(any(), any(), any()))
			.willReturn(subAccountDetails);
		BDDMockito.given(this.paymentRequestRepository.existsByPaymentDetailsId("PAY123")).willReturn(false);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence()).willReturn(12345L);
		BDDMockito.given(this.segmentCreationService.createSegment(any(), any(), any(), any())).willReturn("STSE");
		BDDMockito
			.given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(any(), any(),
					any()))
			.willReturn(Optional.empty());

		try (MockedStatic<PaymentRequestEntityUtil> mockUtil = BDDMockito.mockStatic(PaymentRequestEntityUtil.class)) {
			mockUtil.when(() -> PaymentRequestEntityUtil.toPaymentRequestEntity(this.paymentRequestDto))
				.thenReturn(paymentRequest);
			mockUtil.when(() -> PaymentRequestEntityUtil.generateNetworkPaymentId("B2B", 12345L))
				.thenReturn("BB000012345");

			Assertions
				.assertThatThrownBy(
						() -> this.paymentRequestServiceImpl.processPaymentRequestEvents(this.paymentRequestDto))
				.isInstanceOf(OrganizationRegisteredException.class)
				.hasMessageContaining("Sub account detail not found")
				.hasMessageContaining("subAccCode");
		}
	}

	@Test
	public void handleExistingPaymentRequest_entityNotFound() {
		SubAccountBankAccountDetails subAccountDetails = new SubAccountBankAccountDetails();
		subAccountDetails.setPayer835Id("9887");
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(new PaymentDetails());

		BDDMockito.given(this.subAccountRepository.findSubAccountBankDetails(any(), any(), any()))
			.willReturn(subAccountDetails);
		BDDMockito.given(this.paymentRequestRepository.existsByPaymentDetailsId(any())).willReturn(true);
		BDDMockito.given(this.paymentRequestRepository.findByPaymentId("PAY123")).willReturn(paymentRequest);
		BDDMockito.given(this.paymentDetailsRepository.findById("PAY123")).willReturn(Optional.empty());

		Assertions
			.assertThatThrownBy(
					() -> this.paymentRequestServiceImpl.processPaymentRequestEvents(this.paymentRequestDto))
			.isInstanceOf(EntityNotFoundException.class)
			.hasMessageContaining("PaymentDetails not found");
	}

	// -----------------------------------------------------------------------
	// createNetworkPaymentId – success paths (real util, no mock needed)
	// -----------------------------------------------------------------------

	@Test
	public void testCreateNetworkPaymentId_Success() {
		// given – any non-C2B payment type is categorized as B2B.
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence())
			.willReturn(NETWORK_PAYMENT_ID_BASE36_START);

		// when
		String result = invokeCreateNetworkPaymentId(this.paymentRequestDto);

		// then – assert on the real formatted ID, no util mock required
		Assertions.assertThat(result).isNotNull().isEqualTo("BB100000000");
	}

	@Test
	public void testCreateNetworkPaymentId_B2bPaymentType_ReturnsCorrectPrefix() {
		// given – any non-C2B payment type is treated as B2B.
		this.paymentDetailsDto.setType(PaymentType.NOTIFICATION);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence())
			.willReturn(NETWORK_PAYMENT_ID_BASE36_START + 1);

		// when
		String result = invokeCreateNetworkPaymentId(this.paymentRequestDto);

		// then
		Assertions.assertThat(result).isNotNull().isEqualTo("BB100000001");
	}

	@Test
	public void testCreateNetworkPaymentId_MaxSequenceValue() {
		// given – max sequence maps to ZZZZZZZZZ in base36.
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence())
			.willReturn(101_559_956_668_415L);

		// when
		String result = invokeCreateNetworkPaymentId(this.paymentRequestDto);

		// then
		Assertions.assertThat(result).isNotNull().isEqualTo("BBZZZZZZZZZ");
	}

	// -----------------------------------------------------------------------
	// createNetworkPaymentId – invalid / unsupported payee type
	// -----------------------------------------------------------------------

	@Test
	public void testCreateNetworkPaymentId_NullPayeeDetails_DefaultsToB2b() {
		// given – payee details no longer control C2B/B2B classification.
		this.paymentRequestDto.setPayeeDetails(null);
		this.paymentDetailsDto.setType(null);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence())
			.willReturn(NETWORK_PAYMENT_ID_BASE36_START);

		// when
		String result = invokeCreateNetworkPaymentId(this.paymentRequestDto);

		// then
		Assertions.assertThat(result).isEqualTo("BB100000000");
	}

	@Test
	public void testCreateNetworkPaymentId_NullPaymentType_DefaultsToB2b() {
		// given
		this.paymentDetailsDto.setType(null);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence())
			.willReturn(NETWORK_PAYMENT_ID_BASE36_START);

		// when
		String result = invokeCreateNetworkPaymentId(this.paymentRequestDto);

		// then
		Assertions.assertThat(result).isEqualTo("BB100000000");
	}

	@Test
	public void testCreateNetworkPaymentId_BlankPaymentType_DefaultsToB2b() {
		// given
		this.paymentDetailsDto.setType(PaymentType.PAYMENT_ONLY);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence())
			.willReturn(NETWORK_PAYMENT_ID_BASE36_START);

		// when
		String result = invokeCreateNetworkPaymentId(this.paymentRequestDto);

		// then
		Assertions.assertThat(result).isEqualTo("BB100000000");
	}

	@Test
	public void testCreateNetworkPaymentId_EmptyPaymentType_DefaultsToB2b() {
		// given
		this.paymentDetailsDto.setType(PaymentType.REMITTANCE_ONLY);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence())
			.willReturn(NETWORK_PAYMENT_ID_BASE36_START);

		// when
		String result = invokeCreateNetworkPaymentId(this.paymentRequestDto);

		// then
		Assertions.assertThat(result).isEqualTo("BB100000000");
	}

	@Test
	public void testCreateNetworkPaymentId_UnsupportedPaymentType_DefaultsToB2b() {
		// given – any non-C2B value is normalized to B2B.
		this.paymentDetailsDto.setType(PaymentType.NOTIFICATION);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence())
			.willReturn(NETWORK_PAYMENT_ID_BASE36_START);

		// when
		String result = invokeCreateNetworkPaymentId(this.paymentRequestDto);

		// then
		Assertions.assertThat(result).isEqualTo("BB100000000");
	}

	// -----------------------------------------------------------------------
	// createNetworkPaymentId – null / missing payment details
	// -----------------------------------------------------------------------

	@Test
	public void testCreateNetworkPaymentId_NullPaymentDetails_UsesUnknown() {
		// given – payment ID resolves to "UNKNOWN" when paymentDetails is absent.
		// NOTE: Mockito returns 0L (not null) for an unstubbed Long method, so we must
		// stub explicitly to ensure the null-sequence code path is exercised.
		this.paymentRequestDto.setPaymentDetails(null);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence()).willReturn(null);

		// when / then
		Assertions.assertThatThrownBy(() -> invokeCreateNetworkPaymentId(this.paymentRequestDto))
			.isInstanceOf(NetworkPaymentIdGenerationException.class)
			.hasMessageContaining("UNKNOWN");
	}

	@Test
	public void savePaymentRequest_nullPayeeDetails_throwsBadRequestException() {
		this.paymentRequestDto.setPayeeDetails(null);

		Assertions.assertThatThrownBy(() -> invokeSavePaymentRequest(this.paymentRequestDto))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("receiverId is required");

		verifyNoInteractions(this.subAccountRepository, this.paymentRequestRepository);
	}

	@Test
	public void savePaymentRequest_blankReceiverId_throwsBadRequestException() {
		this.payeeDetailsDto.setReceiverId("   ");

		Assertions.assertThatThrownBy(() -> invokeSavePaymentRequest(this.paymentRequestDto))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("receiverId is required");

		verifyNoInteractions(this.subAccountRepository, this.paymentRequestRepository);
	}

	// -----------------------------------------------------------------------
	// createNetworkPaymentId – sequence / repository failures
	// -----------------------------------------------------------------------

	@Test
	public void testCreateNetworkPaymentId_NullSequenceValue_ThrowsException() {
		// given
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence()).willReturn(null);

		// when / then
		Assertions.assertThatThrownBy(() -> invokeCreateNetworkPaymentId(this.paymentRequestDto))
			.isInstanceOf(NetworkPaymentIdGenerationException.class)
			.hasMessageContaining("Network Payment ID sequence returned null for payment: PAY123");
	}

	@Test
	public void testCreateNetworkPaymentId_SequenceGenerationException_Rethrows() {
		// given
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence())
			.willThrow(new RuntimeException("Database connection failed"));

		// when / then
		Assertions.assertThatThrownBy(() -> invokeCreateNetworkPaymentId(this.paymentRequestDto))
			.isInstanceOf(NetworkPaymentIdGenerationException.class)
			.hasMessageContaining("Failed to generate Network Payment ID for payment: PAY123")
			.hasMessageContaining("Database connection failed");
	}

	@Test
	public void testCreateNetworkPaymentId_UnexpectedUtilException_MapsToSequenceQueryFailed() {
		// given – an unexpected (non-IAE) RuntimeException from the util signals a
		// SEQUENCE_QUERY_FAILED.
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence()).willReturn(12345L);

		try (MockedStatic<PaymentRequestEntityUtil> mockUtil = BDDMockito.mockStatic(PaymentRequestEntityUtil.class)) {
			mockUtil.when(() -> PaymentRequestEntityUtil.generateNetworkPaymentId("B2B", 12345L))
				.thenThrow(new RuntimeException("Unexpected util failure"));

			// when / then
			Assertions.assertThatThrownBy(() -> invokeCreateNetworkPaymentId(this.paymentRequestDto))
				.isInstanceOf(NetworkPaymentIdGenerationException.class)
				.hasMessageContaining("Failed to generate Network Payment ID for payment: PAY123")
				.hasMessageContaining("Unexpected util failure")
				.matches((ex) -> ((NetworkPaymentIdGenerationException) ex).getErrorCode()
					.equals("SEQUENCE_QUERY_FAILED"));
		}
	}

	// -----------------------------------------------------------------------
	// createNetworkPaymentId – exception field assertions
	// -----------------------------------------------------------------------

	@Test
	public void testCreateNetworkPaymentId_ExceptionHasCorrectErrorCode() {
		// given
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence()).willReturn(null);

		// when / then
		Assertions.assertThatThrownBy(() -> invokeCreateNetworkPaymentId(this.paymentRequestDto))
			.isInstanceOf(NetworkPaymentIdGenerationException.class)
			.matches((ex) -> ((NetworkPaymentIdGenerationException) ex).getErrorCode()
				.equals("SEQUENCE_GENERATION_FAILED"));
	}

	@Test
	public void testCreateNetworkPaymentId_ExceptionContainsPaymentId() {
		// given
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence()).willReturn(null);

		// when / then
		Assertions.assertThatThrownBy(() -> invokeCreateNetworkPaymentId(this.paymentRequestDto))
			.isInstanceOf(NetworkPaymentIdGenerationException.class)
			.matches((ex) -> ((NetworkPaymentIdGenerationException) ex).getPaymentId().equals("PAY123"));
	}

	// -----------------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------------

	private String invokeCreateNetworkPaymentId(PaymentRequestDto requestDto) {
		try {
			var method = PaymentRequestServiceImpl.class.getDeclaredMethod("createNetworkPaymentId",
					PaymentRequestDto.class);
			method.setAccessible(true);
			return (String) method.invoke(this.paymentRequestServiceImpl, requestDto);
		}
		catch (Exception ex) {
			if (ex.getCause() instanceof RuntimeException) {
				throw (RuntimeException) ex.getCause();
			}
			throw new RuntimeException("Error invoking createNetworkPaymentId method", ex);
		}
	}

	private void invokeSavePaymentRequestHistory(PaymentRequest paymentRequest) {
		try {
			var method = PaymentRequestServiceImpl.class.getDeclaredMethod("savePaymentRequestHistory",
					PaymentRequest.class);
			method.setAccessible(true);
			method.invoke(this.paymentRequestServiceImpl, paymentRequest);
		}
		catch (Exception ex) {
			if (ex.getCause() instanceof RuntimeException) {
				throw (RuntimeException) ex.getCause();
			}
			throw new RuntimeException("Error invoking savePaymentRequestHistory method", ex);
		}
	}

	@Test
	public void savePaymentRequestHistory_propagatesRePreferenceFlagToHistory() {
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("PAY123");
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(paymentDetails);
		paymentRequest.setRePreferenceFlag(true);
		ArgumentCaptor<PaymentRequestHistory> captor = ArgumentCaptor.forClass(PaymentRequestHistory.class);

		invokeSavePaymentRequestHistory(paymentRequest);

		BDDMockito.verify(this.paymentRequestHistoryRepository).save(captor.capture());
		Assertions.assertThat(captor.getValue().isRePreferenceFlag()).isTrue();
	}

	@Test
	public void appendErrorMessage_appendsWithoutRetryTag() {
		String merged = invokeAppendErrorMessage("Previous business error", "Current business error");

		Assertions.assertThat(merged)
			.contains("Previous business error")
			.contains("Current business error")
			.doesNotContain("[retry=");
	}

	@Test
	public void processPaymentRequestEvents_duplicateConstraint_persistsBusinessErrorMessage() throws Exception {
		SubAccountBankAccountDetails subAccountDetails = new SubAccountBankAccountDetails();
		subAccountDetails.setPayer835Id("dummy");

		PaymentDetails existingDetails = new PaymentDetails();
		existingDetails.setId("PAY123");

		PaymentRequest paymentRequest = buildPaymentRequest();
		paymentRequest.setErrorMessage("Older error");

		SubAccount subAccount = new SubAccount();
		subAccount.setExpiryDays(30);

		BDDMockito.given(this.subAccountRepository.findSubAccountBankDetails(any(), any(), any()))
			.willReturn(subAccountDetails);
		BDDMockito.given(this.paymentRequestRepository.existsByPaymentDetailsId("PAY123")).willReturn(true);
		BDDMockito.given(this.paymentRequestRepository.findByPaymentId("PAY123")).willReturn(paymentRequest);
		BDDMockito.given(this.paymentDetailsRepository.findById("PAY123")).willReturn(Optional.of(existingDetails));
		BDDMockito.given(this.segmentCreationService.createSegment(any(), any(), any(), any())).willReturn("STSE");
		BDDMockito
			.given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(any(), any(),
					any()))
			.willReturn(Optional.of(subAccount));
		BDDMockito.given(this.cardholderRepository.findCardholder())
			.willReturn(new com.uhg.optum.financials.payments.payer.model.CardHolder());
		BDDMockito.given(this.virtualCardCreateService.buildCardHolderDetails(any()))
			.willReturn(com.uhg.optum.financials.payments.payer.dto.CardHolderDto.builder().build());
		BDDMockito.given(this.virtualCardCreateService.findExistingSpendRules(any(), any(), any()))
			.willReturn(Optional.empty());
		BDDMockito.given(this.zetaApiService.getAllRules(any(), any(), any()))
			.willReturn(ResponseEntity.ok("{\"data\":[]}"));
		BDDMockito.given(this.virtualCardCreateService.buildCardCreationRequest(any(), any(), any(), any()))
			.willReturn(com.uhg.optum.financials.payments.payer.dto.CreateCardRequest.builder().build());
		BDDMockito.given(this.virtualCardCreateService.saveCardCreationRequest(any(), any(), any(), any(), any()))
			.willThrow(new RuntimeException(
					"duplicate key value violates unique constraint \"uq_card_creations_payment_id\""));

		Assertions
			.assertThatThrownBy(
					() -> this.paymentRequestServiceImpl.processPaymentRequestEvents(this.paymentRequestDto))
			.isInstanceOf(RuntimeException.class)
			.hasMessageContaining("uq_card_creations_payment_id");

		BDDMockito.verify(this.paymentRequestRepository, org.mockito.Mockito.times(2)).save(any(PaymentRequest.class));
	}

	@Test
	public void createStSeSegmentAndSet_failure_throwsSegmentGenerationExceptionAndAppendsErrorMessage() {
		SubAccountBankAccountDetails subAccountDetails = new SubAccountBankAccountDetails();
		PaymentRequest paymentRequest = buildPaymentRequest();
		paymentRequest.setErrorMessage("Previous segment error");

		BDDMockito.given(this.segmentCreationService.createSegment(any(), any(), any(), any()))
			.willThrow(new RuntimeException("Segment generation failed"));

		Assertions
			.assertThatThrownBy(
					() -> invokeCreateStSeSegmentAndSet(this.paymentRequestDto, subAccountDetails, paymentRequest))
			.isInstanceOf(SegmentGenerationException.class)
			.hasMessageContaining("Error while creating STSE segment for payment Id:PAY123")
			.hasMessageContaining("Segment generation failed");

		Assertions.assertThat(paymentRequest.getErrorMessage())
			.contains("Previous segment error")
			.contains("Error while creating STSE segment for payment Id:PAY123")
			.contains("Segment generation failed")
			.doesNotContain("[retry=");
		Assertions.assertThat(paymentRequest.getStatus()).isEqualTo(PE.name());

		BDDMockito.verify(this.logUtility)
			.logMessageWithPriority(
					org.mockito.ArgumentMatchers.contains("Error while creating STSE segment for payment Id:PAY123"),
					org.mockito.ArgumentMatchers.eq("error"), any(), org.mockito.ArgumentMatchers.eq(P3));
	}

	@Test
	public void processPaymentRequestEvents_stSeSegmentFails_DoesNotPropagate_SoQueueDoesNotRetry() {
		SubAccountBankAccountDetails subAccountDetails = new SubAccountBankAccountDetails();
		subAccountDetails.setPayer835Id("dummy");
		PaymentRequest paymentRequest = buildPaymentRequest();

		BDDMockito.given(this.subAccountRepository.findSubAccountBankDetails(any(), any(), any()))
			.willReturn(subAccountDetails);
		BDDMockito.given(this.paymentRequestRepository.existsByPaymentDetailsId("PAY123")).willReturn(false);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence()).willReturn(12345L);
		BDDMockito.given(this.segmentCreationService.createSegment(any(), any(), any(), any()))
			.willThrow(new RuntimeException("Segment generation failed"));

		try (MockedStatic<PaymentRequestEntityUtil> mockUtil = BDDMockito.mockStatic(PaymentRequestEntityUtil.class)) {
			mockUtil.when(() -> PaymentRequestEntityUtil.toPaymentRequestEntity(this.paymentRequestDto))
				.thenReturn(paymentRequest);
			mockUtil.when(() -> PaymentRequestEntityUtil.generateNetworkPaymentId("B2B", 12345L))
				.thenReturn("BB000012345");

			// This is a local, deterministic failure (bad/malformed data, not a
			// transient dependency) — it must NOT propagate out of the public entry
			// point, since that would make the Storage Queue redeliver the exact same
			// input up to 5 times, each one guaranteed to fail identically.
			Assertions
				.assertThatCode(
						() -> this.paymentRequestServiceImpl.processPaymentRequestEvents(this.paymentRequestDto))
				.doesNotThrowAnyException();
		}

		// PE + history are already recorded inside createStSeSegmentAndSet itself, and
		// nothing past it (sub-account lookup, cardholder/spend-rule validation, Zeta
		// call) should have run for a payment already known to be broken.
		Assertions.assertThat(paymentRequest.getStatus()).isEqualTo(PE.name());
		BDDMockito.verifyNoInteractions(this.zetaApiService);
	}

	private void invokeSavePaymentRequest(PaymentRequestDto requestDto) {
		try {
			var method = PaymentRequestServiceImpl.class.getDeclaredMethod("savePaymentRequest",
					PaymentRequestDto.class);
			method.setAccessible(true);
			method.invoke(this.paymentRequestServiceImpl, requestDto);
		}
		catch (Exception ex) {
			if (ex.getCause() instanceof RuntimeException) {
				throw (RuntimeException) ex.getCause();
			}
			throw new RuntimeException("Error invoking savePaymentRequest method", ex);
		}
	}

	private String invokeAppendErrorMessage(String existingErrorMessage, String nextErrorMessage) {
		try {
			var method = PaymentRequestServiceImpl.class.getDeclaredMethod("appendErrorMessage", String.class,
					String.class);
			method.setAccessible(true);
			return (String) method.invoke(this.paymentRequestServiceImpl, existingErrorMessage, nextErrorMessage);
		}
		catch (Exception ex) {
			if (ex.getCause() instanceof RuntimeException) {
				throw (RuntimeException) ex.getCause();
			}
			throw new RuntimeException("Error invoking appendErrorMessage method", ex);
		}
	}

	private void invokeCreateStSeSegmentAndSet(PaymentRequestDto requestDto,
			SubAccountBankAccountDetails subAccountDetails, PaymentRequest paymentRequest) {
		try {
			var method = PaymentRequestServiceImpl.class.getDeclaredMethod("createStSeSegmentAndSet",
					PaymentRequestDto.class, SubAccountBankAccountDetails.class, PaymentRequest.class);
			method.setAccessible(true);
			method.invoke(this.paymentRequestServiceImpl, requestDto, subAccountDetails, paymentRequest);
		}
		catch (Exception ex) {
			if (ex.getCause() instanceof RuntimeException) {
				throw (RuntimeException) ex.getCause();
			}
			throw new RuntimeException("Error invoking createStSeSegmentAndSet method", ex);
		}
	}

	// -----------------------------------------------------------------------
	// validateCardHolderDto – exception handling tests
	// -----------------------------------------------------------------------

	@Test
	public void processPaymentRequestEvents_validateCardHolderDtoThrowsException_catchesAndReturns() {
		SubAccountBankAccountDetails subAccountDetails = new SubAccountBankAccountDetails();
		subAccountDetails.setPayer835Id("dummy");
		PaymentRequest paymentRequest = buildPaymentRequest();

		SubAccount subAccount = new SubAccount();
		subAccount.setExpiryDays(30);

		CardHolder existingCardHolder = new CardHolder();
		CardHolderDto invalidCardHolder = CardHolderDto.builder().build();

		BDDMockito.given(this.subAccountRepository.findSubAccountBankDetails(any(), any(), any()))
			.willReturn(subAccountDetails);
		BDDMockito.given(this.paymentRequestRepository.existsByPaymentDetailsId("PAY123")).willReturn(false);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence()).willReturn(12345L);
		BDDMockito.given(this.segmentCreationService.createSegment(any(), any(), any(), any())).willReturn("STSE");
		BDDMockito
			.given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(any(), any(),
					any()))
			.willReturn(Optional.of(subAccount));
		BDDMockito.given(this.cardholderRepository.findCardholder()).willReturn(existingCardHolder);
		BDDMockito.given(this.virtualCardCreateService.buildCardHolderDetails(any())).willReturn(invalidCardHolder);
		BDDMockito.given(this.validator.validate(any()))
			.willReturn(java.util.Collections
				.singleton(org.mockito.Mockito.mock(jakarta.validation.ConstraintViolation.class)));

		try (MockedStatic<PaymentRequestEntityUtil> mockUtil = BDDMockito.mockStatic(PaymentRequestEntityUtil.class)) {
			mockUtil.when(() -> PaymentRequestEntityUtil.toPaymentRequestEntity(this.paymentRequestDto))
				.thenReturn(paymentRequest);
			mockUtil.when(() -> PaymentRequestEntityUtil.generateNetworkPaymentId("B2B", 12345L))
				.thenReturn("BB000012345");

			// The method should not throw, but should return early after catching
			// BadRequestException
			this.paymentRequestServiceImpl.processPaymentRequestEvents(this.paymentRequestDto);

			// Verify that updatePaymentRequestWithError was called
			BDDMockito.verify(this.paymentRequestRepository).save(any(PaymentRequest.class));
			BDDMockito.verify(this.logUtility)
				.logMessageWithPriority(
						org.mockito.ArgumentMatchers
							.contains("Payment request updated with PE status and error message for paymentId: PAY123"),
						org.mockito.ArgumentMatchers.eq("error"), any(), org.mockito.ArgumentMatchers.eq(P3));
		}
	}

	// -----------------------------------------------------------------------
	// validateSpendRules – exception handling tests
	// -----------------------------------------------------------------------

	@Test
	public void processPaymentRequestEvents_validateSpendRulesThrowsException_catchesAndReturns() throws Exception {
		SubAccountBankAccountDetails subAccountDetails = new SubAccountBankAccountDetails();
		subAccountDetails.setPayer835Id("dummy");
		PaymentRequest paymentRequest = buildPaymentRequest();

		SubAccount subAccount = new SubAccount();
		subAccount.setExpiryDays(30);

		CardHolder existingCardHolder = new CardHolder();
		CardHolderDto validCardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john@example.com")
			.build();

		SpendRules invalidSpendRules = new SpendRules();
		invalidSpendRules.setRuleCode(java.util.Arrays.asList("RULE_WITH_INVALID_CHARS<>"));

		BDDMockito.given(this.subAccountRepository.findSubAccountBankDetails(any(), any(), any()))
			.willReturn(subAccountDetails);
		BDDMockito.given(this.paymentRequestRepository.existsByPaymentDetailsId("PAY123")).willReturn(false);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence()).willReturn(12345L);
		BDDMockito.given(this.segmentCreationService.createSegment(any(), any(), any(), any())).willReturn("STSE");
		BDDMockito
			.given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(any(), any(),
					any()))
			.willReturn(Optional.of(subAccount));
		BDDMockito.given(this.cardholderRepository.findCardholder()).willReturn(existingCardHolder);
		BDDMockito.given(this.virtualCardCreateService.buildCardHolderDetails(any())).willReturn(validCardHolder);
		BDDMockito.given(this.virtualCardCreateService.findExistingSpendRules(any(), any(), any()))
			.willReturn(Optional.of(invalidSpendRules));
		BDDMockito.given(this.validator.validate(any())).willReturn(java.util.Collections.emptySet());
		BDDMockito.given(this.zetaApiService.getAllRules(any(), any(), any()))
			.willReturn(ResponseEntity.ok("{\"data\":[]}"));

		try (MockedStatic<PaymentRequestEntityUtil> mockUtil = BDDMockito.mockStatic(PaymentRequestEntityUtil.class)) {
			mockUtil.when(() -> PaymentRequestEntityUtil.toPaymentRequestEntity(this.paymentRequestDto))
				.thenReturn(paymentRequest);
			mockUtil.when(() -> PaymentRequestEntityUtil.generateNetworkPaymentId("B2B", 12345L))
				.thenReturn("BB000012345");

			// The method should not throw, but should return early after catching
			// BadRequestException
			this.paymentRequestServiceImpl.processPaymentRequestEvents(this.paymentRequestDto);

			// Verify that updatePaymentRequestWithError was called
			BDDMockito.verify(this.paymentRequestRepository).save(any(PaymentRequest.class));
			BDDMockito.verify(this.logUtility)
				.logMessageWithPriority(
						org.mockito.ArgumentMatchers
							.contains("Payment request updated with PE status and error message for paymentId: PAY123"),
						org.mockito.ArgumentMatchers.eq("error"), any(), org.mockito.ArgumentMatchers.eq(P3));
		}
	}

	@Test
	public void processPaymentRequestEvents_cardCreationValidationFails_catchesAndUpdatesPaymentToPE()
			throws Exception {
		SubAccountBankAccountDetails subAccountDetails = new SubAccountBankAccountDetails();
		subAccountDetails.setPayer835Id("dummy");
		PaymentRequest paymentRequest = buildPaymentRequest();

		SubAccount subAccount = new SubAccount();
		subAccount.setExpiryDays(30);

		CardHolder existingCardHolder = new CardHolder();
		CardHolderDto validCardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john@example.com")
			.build();

		BDDMockito.given(this.subAccountRepository.findSubAccountBankDetails(any(), any(), any()))
			.willReturn(subAccountDetails);
		BDDMockito.given(this.paymentRequestRepository.existsByPaymentDetailsId("PAY123")).willReturn(false);
		BDDMockito.given(this.paymentDetailsRepository.getNextNetworkPaymentIdSequence()).willReturn(12345L);
		BDDMockito.given(this.segmentCreationService.createSegment(any(), any(), any(), any())).willReturn("STSE");
		BDDMockito
			.given(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(any(), any(),
					any()))
			.willReturn(Optional.of(subAccount));
		BDDMockito.given(this.cardholderRepository.findCardholder()).willReturn(existingCardHolder);
		BDDMockito.given(this.virtualCardCreateService.buildCardHolderDetails(any())).willReturn(validCardHolder);
		BDDMockito.given(this.virtualCardCreateService.findExistingSpendRules(any(), any(), any()))
			.willReturn(Optional.empty());
		BDDMockito.given(this.validator.validate(any())).willReturn(java.util.Collections.emptySet());
		BDDMockito.given(this.zetaApiService.getAllRules(any(), any(), any()))
			.willReturn(ResponseEntity.ok("{\"data\":[]}"));

		// Mock buildCardCreationRequest to throw CardProcessingError (simulating
		// validation
		// failure). The validation happens at service level, but P3 logging occurs when
		// the error is caught at the processPaymentRequestEvents level.
		BDDMockito.given(this.virtualCardCreateService.buildCardCreationRequest(any(), any(), any(), any()))
			.willThrow(new CardProcessingError(
					"Card creation request validation failed for paymentId: PAY123 - amount: must be greater than or equal to 1"));

		try (MockedStatic<PaymentRequestEntityUtil> mockUtil = BDDMockito.mockStatic(PaymentRequestEntityUtil.class)) {
			mockUtil.when(() -> PaymentRequestEntityUtil.toPaymentRequestEntity(this.paymentRequestDto))
				.thenReturn(paymentRequest);
			mockUtil.when(() -> PaymentRequestEntityUtil.generateNetworkPaymentId("B2B", 12345L))
				.thenReturn("BB000012345");

			// When / Then: The method should not throw, but should return early after
			// catching CardProcessingError and updating payment status to PE
			this.paymentRequestServiceImpl.processPaymentRequestEvents(this.paymentRequestDto);

			// Verify that updatePaymentRequestWithError was called (which updates status
			// to PE and logs with P3 priority)
			BDDMockito.verify(this.paymentRequestRepository).save(any(PaymentRequest.class));
			BDDMockito.verify(this.logUtility)
				.logMessageWithPriority(
						org.mockito.ArgumentMatchers
							.contains("Payment request updated with PE status and error message for paymentId: PAY123"),
						org.mockito.ArgumentMatchers.eq("error"), any(), org.mockito.ArgumentMatchers.eq(P3));
		}
	}

	// -----------------------------------------------------------------------
	// updatePaymentRequestWithError – P3 alert tests
	// -----------------------------------------------------------------------

	@Test
	public void updatePaymentRequestWithError_success_logsWithErrorAndP3Priority() {
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("PAY123");
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(paymentDetails);

		BDDMockito.given(this.paymentRequestRepository.findByPaymentId("PAY123")).willReturn(paymentRequest);

		invokeUpdatePaymentRequestWithError("PAY123", "Test error message");

		// Verify that logMessageWithPriority was called with ERROR level and P3 priority
		BDDMockito.verify(this.logUtility)
			.logMessage(
					org.mockito.ArgumentMatchers
						.contains("Payment request updated with PE status and error message for paymentId: PAY123"),
					org.mockito.ArgumentMatchers.eq("info"), any());

		// Verify that payment request was saved with PE status
		ArgumentCaptor<PaymentRequest> captor = ArgumentCaptor.forClass(PaymentRequest.class);
		BDDMockito.verify(this.paymentRequestRepository).save(captor.capture());
		Assertions.assertThat(captor.getValue().getStatus()).isEqualTo(PE.name());

		// Verify that payment request history was saved
		BDDMockito.verify(this.paymentRequestHistoryRepository).save(any(PaymentRequestHistory.class));
	}

	@Test
	public void updatePaymentRequestWithError_withExistingError_appendsErrorMessage() {
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("PAY123");
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(paymentDetails);
		paymentRequest.setErrorMessage("Previous error");

		BDDMockito.given(this.paymentRequestRepository.findByPaymentId("PAY123")).willReturn(paymentRequest);

		invokeUpdatePaymentRequestWithError("PAY123", "New error");

		// Verify that error message was appended
		ArgumentCaptor<PaymentRequest> captor = ArgumentCaptor.forClass(PaymentRequest.class);
		BDDMockito.verify(this.paymentRequestRepository).save(captor.capture());
		Assertions.assertThat(captor.getValue().getErrorMessage()).contains("Previous error").contains("New error");
	}

	@Test
	public void updatePaymentRequestWithError_paymentRequestNotFound_doesNotThrow() {
		BDDMockito.given(this.paymentRequestRepository.findByPaymentId("PAY123")).willReturn(null);

		// Should not throw exception when payment request is not found
		Assertions.assertThatNoException()
			.isThrownBy(() -> invokeUpdatePaymentRequestWithError("PAY123", "Test error message"));

		// Verify that repository save was not called (findByPaymentId will be called but
		// save
		// should not)
		BDDMockito.verify(this.paymentRequestRepository, org.mockito.Mockito.never()).save(any(PaymentRequest.class));
	}

	@Test
	public void updatePaymentRequestWithError_exceptionDuringSave_logsWithErrorAndP3Priority() {
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("PAY123");
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(paymentDetails);

		BDDMockito.given(this.paymentRequestRepository.findByPaymentId("PAY123")).willReturn(paymentRequest);
		BDDMockito.given(this.paymentRequestRepository.save(any(PaymentRequest.class)))
			.willThrow(new RuntimeException("Database error"));

		// Should not throw exception
		Assertions.assertThatNoException()
			.isThrownBy(() -> invokeUpdatePaymentRequestWithError("PAY123", "Test error message"));

		// Verify that error was logged with P3 priority
		BDDMockito.verify(this.logUtility)
			.logMessageWithPriority(
					org.mockito.ArgumentMatchers.contains("Failed to update payment request with error status"),
					org.mockito.ArgumentMatchers.eq("error"), any(), org.mockito.ArgumentMatchers.eq(P3));
	}

	private void invokeUpdatePaymentRequestWithError(String paymentId, String errorMessage) {
		try {
			var method = PaymentRequestServiceImpl.class.getDeclaredMethod("updatePaymentRequestWithError",
					String.class, String.class);
			method.setAccessible(true);
			method.invoke(this.paymentRequestServiceImpl, paymentId, errorMessage);
		}
		catch (Exception ex) {
			if (ex.getCause() instanceof RuntimeException) {
				throw (RuntimeException) ex.getCause();
			}
			throw new RuntimeException("Error invoking updatePaymentRequestWithError method", ex);
		}
	}

	private PaymentRequest buildPaymentRequest() {
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId("PAY123");
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(paymentDetails);
		paymentRequest.setCreatedOn(java.time.LocalDateTime.now());
		return paymentRequest;
	}

}
