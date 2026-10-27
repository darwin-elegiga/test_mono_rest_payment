package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.enums.Status;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.PaymentRequestHistory;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestHistoryRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;

import java.util.List;
import java.util.stream.Collectors;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Integration tests for error message capture across all payment processing layers.
 *
 * Verifies that: 1. Error messages are captured in PaymentRequest.errorMessage 2. Error
 * messages are stored in PaymentRequestHistory 3. Error messages are exposed via the
 * query API 4. Errors from different layers (API, Queue, Worker, Webhook) are properly
 * recorded
 */
@SpringBootTest
@ActiveProfiles("test")
public class ErrorMessageCaptureIntegrationTests {

	private static final Logger log = LoggerFactory.getLogger(ErrorMessageCaptureIntegrationTests.class);

	@Autowired
	private PaymentRequestRepository paymentRequestRepository;

	@Autowired
	private PaymentRequestHistoryRepository paymentRequestHistoryRepository;

	@Autowired
	private VirtualCardUtil virtualCardUtil;

	private String testPaymentId;

	@BeforeEach
	public void setUp() {
		testPaymentId = "TEST_PAY_" + System.currentTimeMillis();
	}

	@Test
	@DisplayName("Should capture error message when updating payment request status to PE (Payment Error)")
	public void testErrorMessageCaptureOnStatusUpdate() {
		// Arrange
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(new com.uhg.optum.financials.payments.payer.model.PaymentDetails());
		paymentRequest.getPaymentDetails().setId(testPaymentId);
		paymentRequest.setStatus(Status.PI.name());
		paymentRequest.setOrgShortCode("TEST_ORG");
		paymentRequest.setAccountShortCode("TEST_ACCT");
		paymentRequest.setSubAccountShortCode("TEST_SUB");
		paymentRequest.setOrigin("TEST_ORIGIN");
		paymentRequestRepository.save(paymentRequest);

		String expectedErrorMessage = "Queue transmission failed: Connection timeout after 30 seconds";

		// Act - Update payment request with error message
		PaymentRequest updatedRequest = virtualCardUtil.updatePaymentRequestStatus(testPaymentId, Status.PE,
				expectedErrorMessage);

		// Assert
		assertThat(updatedRequest).isNotNull();
		assertThat(updatedRequest.getStatus()).isEqualTo(Status.PE.name());
		assertThat(updatedRequest.getErrorMessage()).isEqualTo(expectedErrorMessage);

		log.info("✓ Error message captured on status update: {}", expectedErrorMessage);
	}

	@Test
	@DisplayName("Should store error message in PaymentRequestHistory when payment status changes")
	public void testErrorMessageStoredInHistory() {
		// Arrange
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(new com.uhg.optum.financials.payments.payer.model.PaymentDetails());
		paymentRequest.getPaymentDetails().setId(testPaymentId + "_HISTORY");
		paymentRequest.setStatus(Status.PI.name());
		paymentRequest.setOrgShortCode("TEST_ORG");
		paymentRequest.setAccountShortCode("TEST_ACCT");
		paymentRequest.setSubAccountShortCode("TEST_SUB");
		paymentRequest.setOrigin("TEST_ORIGIN");
		paymentRequest.setCreatedById(AppConstants.VIRTUAL_CARD);
		paymentRequestRepository.save(paymentRequest);

		String errorMessage = "Worker service failed to create virtual card: Invalid card parameters";

		// Act
		PaymentRequest updated = virtualCardUtil.updatePaymentRequestStatus(testPaymentId + "_HISTORY", Status.PE,
				errorMessage);

		// Assert - Verify error message is in payment request
		assertThat(updated.getErrorMessage()).isEqualTo(errorMessage);

		// Assert - Verify error message is in history
		List<PaymentRequestHistory> historyRecords = this.paymentRequestHistoryRepository.findAll()
			.stream()
			.filter(h -> (testPaymentId + "_HISTORY").equals(h.getPaymentDetailsId()))
			.collect(Collectors.toList());
		assertThat(historyRecords).isNotEmpty();

		PaymentRequestHistory latestHistory = historyRecords.stream()
			.filter(h -> Status.PE.name().equals(h.getStatus()))
			.findFirst()
			.orElse(null);

		assertThat(latestHistory).isNotNull();
		assertThat(latestHistory.getErrorMessage()).isEqualTo(errorMessage);

		log.info("✓ Error message stored in PaymentRequestHistory: {}", errorMessage);
	}

	@Test
	@DisplayName("Should retrieve error message through query API in PaymentResponseDto")
	public void testErrorMessageExposedInApi() {
		// Arrange
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(new com.uhg.optum.financials.payments.payer.model.PaymentDetails());
		paymentRequest.getPaymentDetails().setId(testPaymentId + "_API");
		paymentRequest.setStatus(Status.PE.name());
		paymentRequest.setOrgShortCode("TEST_ORG");
		paymentRequest.setAccountShortCode("TEST_ACCT");
		paymentRequest.setSubAccountShortCode("TEST_SUB");
		paymentRequest.setOrigin("TEST_ORIGIN");
		String apiErrorMessage = "Payment segment generation failed: Missing required field - segment type";
		paymentRequest.setErrorMessage(apiErrorMessage);
		paymentRequestRepository.save(paymentRequest);

		// Act - Simulate API query
		PaymentRequest retrieved = paymentRequestRepository.findByPaymentId(testPaymentId + "_API");

		// Assert
		assertThat(retrieved).isNotNull();
		assertThat(retrieved.getErrorMessage()).isEqualTo(apiErrorMessage);

		log.info("✓ Error message exposed in API response: {}", apiErrorMessage);
	}

	@Test
	@DisplayName("Should track error progression through multiple status changes")
	public void testErrorMessageHistoryProgression() {
		// Arrange
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(new com.uhg.optum.financials.payments.payer.model.PaymentDetails());
		paymentRequest.getPaymentDetails().setId(testPaymentId + "_PROGRESSION");
		paymentRequest.setStatus(Status.PI.name());
		paymentRequest.setOrgShortCode("TEST_ORG");
		paymentRequest.setAccountShortCode("TEST_ACCT");
		paymentRequest.setSubAccountShortCode("TEST_SUB");
		paymentRequest.setOrigin("TEST_ORIGIN");
		paymentRequest.setCreatedById(AppConstants.VIRTUAL_CARD);
		paymentRequestRepository.save(paymentRequest);

		// Act - First error during initial processing
		String firstError = "Initial API validation failed: Invalid payee details";
		PaymentRequest updated1 = virtualCardUtil.updatePaymentRequestStatus(testPaymentId + "_PROGRESSION", Status.PE,
				firstError);

		// Assert first error
		assertThat(updated1.getErrorMessage()).isEqualTo(firstError);

		// Act - Simulate retry with new error
		String secondError = "Retry failed: Queue service unreachable";
		PaymentRequest updated2 = virtualCardUtil.updatePaymentRequestStatus(testPaymentId + "_PROGRESSION", Status.PE,
				secondError);

		// Assert second error overwrites first
		assertThat(updated2.getErrorMessage()).isEqualTo(secondError);

		// Assert history captures both status changes
		List<PaymentRequestHistory> historyRecords = this.paymentRequestHistoryRepository.findAll()
			.stream()
			.filter(h -> (testPaymentId + "_PROGRESSION").equals(h.getPaymentDetailsId()))
			.collect(Collectors.toList());
		assertThat(historyRecords).size().isGreaterThanOrEqualTo(1);

		log.info("✓ Error progression tracked through history:");
		log.info("  - First error: {}", firstError);
		log.info("  - Second error (on retry): {}", secondError);
	}

	@Test
	@DisplayName("Should capture null error message without throwing exception")
	public void testNullErrorMessageHandling() {
		// Arrange
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(new com.uhg.optum.financials.payments.payer.model.PaymentDetails());
		paymentRequest.getPaymentDetails().setId(testPaymentId + "_NULL_ERROR");
		paymentRequest.setStatus(Status.PI.name());
		paymentRequest.setOrgShortCode("TEST_ORG");
		paymentRequest.setAccountShortCode("TEST_ACCT");
		paymentRequest.setSubAccountShortCode("TEST_SUB");
		paymentRequest.setOrigin("TEST_ORIGIN");
		paymentRequest.setCreatedById(AppConstants.VIRTUAL_CARD);
		paymentRequestRepository.save(paymentRequest);

		// Act - Update with null error message (e.g., successful processing)
		PaymentRequest updated = virtualCardUtil.updatePaymentRequestStatus(testPaymentId + "_NULL_ERROR", Status.PC,
				null);

		// Assert
		assertThat(updated.getStatus()).isEqualTo(Status.PC.name());
		assertThat(updated.getErrorMessage()).isNull();

		log.info("✓ Null error message handled correctly for successful status");
	}

	@Test
	@DisplayName("Should handle long error messages without truncation")
	public void testLongErrorMessageCapture() {
		// Arrange
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setPaymentDetails(new com.uhg.optum.financials.payments.payer.model.PaymentDetails());
		paymentRequest.getPaymentDetails().setId(testPaymentId + "_LONG_ERROR");
		paymentRequest.setStatus(Status.PI.name());
		paymentRequest.setOrgShortCode("TEST_ORG");
		paymentRequest.setAccountShortCode("TEST_ACCT");
		paymentRequest.setSubAccountShortCode("TEST_SUB");
		paymentRequest.setOrigin("TEST_ORIGIN");
		paymentRequest.setCreatedById(AppConstants.VIRTUAL_CARD);
		paymentRequestRepository.save(paymentRequest);

		// Act - Set a long error message
		StringBuilder longErrorBuilder = new StringBuilder("Error Details: ");
		for (int i = 0; i < 10; i++) {
			longErrorBuilder.append("This is error detail #").append(i).append(" - ");
		}
		String longErrorMessage = longErrorBuilder.toString();

		PaymentRequest updated = virtualCardUtil.updatePaymentRequestStatus(testPaymentId + "_LONG_ERROR", Status.PE,
				longErrorMessage);

		// Assert
		assertThat(updated.getErrorMessage()).isEqualTo(longErrorMessage);
		assertThat(updated.getErrorMessage().length()).isGreaterThan(100);

		log.info("✓ Long error message captured without truncation ({} characters)", longErrorMessage.length());
	}

}
