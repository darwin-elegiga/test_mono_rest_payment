package com.uhg.optum.financials.payments.payer.dto;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import jakarta.validation.Validation;
import jakarta.validation.Validator;
import jakarta.validation.ValidatorFactory;

import java.util.Arrays;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Unit tests for CreateCardRequest DTO validation.
 *
 * @author Payments Team
 */
class CreateCardRequestTests {

	private Validator validator;

	@BeforeEach
	void setUp() {
		ValidatorFactory factory = Validation.buildDefaultValidatorFactory();
		this.validator = factory.getValidator();
	}

	@Test
	void testValidCreateCardRequest() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(541212L)
			.amount(1000000L)
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John")
				.lastName("Doe")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.spendRules(Arrays.asList("RS1-0001", "RS2-0004"))
			.autoActivateCard(true)
			.build();

		assertThat(this.validator.validate(request)).isEmpty();
	}

	@Test
	void testInvalidRequestIdNull() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(null)
			.bin(541212L)
			.amount(1000000L)
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John")
				.lastName("Doe")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.build();

		var violations = this.validator.validate(request);
		assertThat(violations).isNotEmpty();
		assertThat(violations.stream().map((v) -> v.getMessage()))
			.anyMatch((msg) -> msg.contains("requestId cannot be null"));
	}

	@Test
	void testInvalidAmountMinViolation() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(541212L)
			.amount(0L)
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John")
				.lastName("Doe")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.build();

		var violations = this.validator.validate(request);
		assertThat(violations).isNotEmpty();
		assertThat(violations.stream().map((v) -> v.getMessage()))
			.anyMatch((msg) -> msg.contains("must be greater than or equal to 1"));
	}

	@Test
	void testInvalidAmountMaxViolation() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(541212L)
			.amount(10000000000L) // Exceeds max of 9,999,999,999
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John")
				.lastName("Doe")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.build();

		var violations = this.validator.validate(request);
		assertThat(violations).isNotEmpty();
		assertThat(violations.stream().map((v) -> v.getMessage()))
			.anyMatch((msg) -> msg.contains("must be less than or equal to 9999999999"));
	}

	@Test
	void testValidAmountAtMinimum() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(541212L)
			.amount(1L)
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John")
				.lastName("Doe")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.build();

		assertThat(this.validator.validate(request)).isEmpty();
	}

	@Test
	void testValidAmountAtMaximum() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(541212L)
			.amount(9999999999L)
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John")
				.lastName("Doe")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.build();

		assertThat(this.validator.validate(request)).isEmpty();
	}

	@Test
	void testInvalidSpendRulesContainsDisallowedCharacters() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(541212L)
			.amount(1000000L)
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John")
				.lastName("Doe")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.spendRules(Arrays.asList("RS1<-0001", "RS2-0004"))
			.build();

		var violations = this.validator.validate(request);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidCardHolderFirstNameFreeTextViolation() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(541212L)
			.amount(1000000L)
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John<Script>")
				.lastName("Doe")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.build();

		var violations = this.validator.validate(request);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidCardHolderLastNameFreeTextViolation() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(541212L)
			.amount(1000000L)
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John")
				.lastName("Doe&Co")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.build();

		var violations = this.validator.validate(request);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testValidNullSpendRules() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(541212L)
			.amount(1000000L)
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John")
				.lastName("Doe")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.spendRules(null)
			.build();

		assertThat(this.validator.validate(request)).isEmpty();
	}

	@Test
	void testValidEmptySpendRules() {
		CreateCardRequest request = CreateCardRequest.builder()
			.requestId(UUID.randomUUID())
			.bin(541212L)
			.amount(1000000L)
			.expiry(new Expiry("02", "28"))
			.cardHolder(CardHolderDto.builder()
				.firstName("John")
				.lastName("Doe")
				.email("john@example.com")
				.billingAddress(BillingAddress.builder()
					.line1("123 Main St")
					.city("New York")
					.country("US")
					.postalCode("10001")
					.build())
				.build())
			.spendRules(Arrays.asList())
			.build();

		assertThat(this.validator.validate(request)).isEmpty();
	}

}
