package com.uhg.optum.financials.payments.payer.dto;

import jakarta.validation.ConstraintViolation;
import jakarta.validation.Validation;
import jakarta.validation.Validator;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import java.util.Set;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Unit tests for ResetCardRequest validation. Verifies shared free-text validation and
 * required field constraints.
 *
 * @author Payments Network Team
 */
@DisplayName("ResetCardRequest Validation Tests")
class ResetCardRequestTests {

	private static Validator validator;

	private ResetCardRequest request;

	@BeforeAll
	static void setUpValidator() {
		validator = Validation.buildDefaultValidatorFactory().getValidator();
	}

	@BeforeEach
	void setUp() {
		this.request = new ResetCardRequest();
	}

	@Test
	@DisplayName("Should accept valid reset request values")
	void testValidResetCardRequest() {
		this.request.setReceiptId("receipt-12345");
		this.request.setResetPurpose("Payer called to drop holds");
		this.request.setPaymentId("payment-12345");
		this.request.setCardGuid(UUID.fromString("550e8400-e29b-41d4-a716-446655440000"));

		Set<ConstraintViolation<ResetCardRequest>> violations = validator.validate(this.request);
		assertThat(violations).isEmpty();
	}

	@Test
	@DisplayName("Should reject receiptId over 255 characters")
	void testReceiptIdExceedsMaxLength() {
		this.request.setReceiptId("A".repeat(256));
		this.request.setResetPurpose("Payer called to drop holds");
		this.request.setPaymentId("payment-12345");

		Set<ConstraintViolation<ResetCardRequest>> violations = validator.validate(this.request);
		assertThat(violations).isNotEmpty()
			.extracting(ConstraintViolation::getMessage)
			.anyMatch((message) -> message.contains("invalid characters or exceeds 255 characters"));
	}

	@Test
	@DisplayName("Should reject resetPurpose over 255 characters")
	void testResetPurposeExceedsMaxLength() {
		this.request.setReceiptId("receipt-12345");
		this.request.setResetPurpose("B".repeat(256));
		this.request.setPaymentId("payment-12345");

		Set<ConstraintViolation<ResetCardRequest>> violations = validator.validate(this.request);
		assertThat(violations).isNotEmpty()
			.extracting(ConstraintViolation::getMessage)
			.anyMatch((message) -> message.contains("invalid characters or exceeds 255 characters"));
	}

	@Test
	@DisplayName("Should reject receiptId with shared disallowed characters")
	void testReceiptIdContainsDisallowedCharacters() {
		this.request.setReceiptId("receipt<123>");
		this.request.setResetPurpose("Payer called to drop holds");
		this.request.setPaymentId("payment-12345");

		Set<ConstraintViolation<ResetCardRequest>> violations = validator.validate(this.request);
		assertThat(violations).isNotEmpty();
	}

	@Test
	@DisplayName("Should reject resetPurpose with shared disallowed characters")
	void testResetPurposeContainsDisallowedCharacters() {
		this.request.setReceiptId("receipt-12345");
		this.request.setResetPurpose("Drop holds / reset");
		this.request.setPaymentId("payment-12345");

		Set<ConstraintViolation<ResetCardRequest>> violations = validator.validate(this.request);
		assertThat(violations).isNotEmpty();
	}

	@Test
	@DisplayName("Should reject null receiptId")
	void testNullReceiptId() {
		this.request.setReceiptId(null);
		this.request.setResetPurpose("Payer called to drop holds");
		this.request.setPaymentId("payment-12345");

		Set<ConstraintViolation<ResetCardRequest>> violations = validator.validate(this.request);
		assertThat(violations).isNotEmpty()
			.extracting(ConstraintViolation::getMessage)
			.contains("receiptId cannot be null or empty.");
	}

	@Test
	@DisplayName("Should reject null resetPurpose")
	void testNullResetPurpose() {
		this.request.setReceiptId("receipt-12345");
		this.request.setResetPurpose(null);
		this.request.setPaymentId("payment-12345");

		Set<ConstraintViolation<ResetCardRequest>> violations = validator.validate(this.request);
		assertThat(violations).isNotEmpty()
			.extracting(ConstraintViolation::getMessage)
			.contains("reason cannot be null or empty.");
	}

	@Test
	@DisplayName("Should reject null paymentId")
	void testNullPaymentId() {
		this.request.setReceiptId("receipt-12345");
		this.request.setResetPurpose("Payer called to drop holds");
		this.request.setPaymentId(null);

		Set<ConstraintViolation<ResetCardRequest>> violations = validator.validate(this.request);
		assertThat(violations).isNotEmpty()
			.extracting(ConstraintViolation::getMessage)
			.contains("paymentId cannot be null or empty.");
	}

	@Test
	@DisplayName("Should allow 255-character receiptId and resetPurpose")
	void testBoundaryLengthValues() {
		this.request.setReceiptId("R".repeat(255));
		this.request.setResetPurpose("P".repeat(255));
		this.request.setPaymentId("payment-12345");

		Set<ConstraintViolation<ResetCardRequest>> violations = validator.validate(this.request);
		assertThat(violations).isEmpty();
	}

}
