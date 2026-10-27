package com.uhg.optum.financials.payments.payer.dto;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import jakarta.validation.Validation;
import jakarta.validation.Validator;
import jakarta.validation.ValidatorFactory;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Unit tests for CardHolderDto validation.
 *
 * @author Payments Team
 */
class CardHolderDtoTests {

	private Validator validator;

	@BeforeEach
	void setUp() {
		ValidatorFactory factory = Validation.buildDefaultValidatorFactory();
		this.validator = factory.getValidator();
	}

	@Test
	void testValidCardHolder() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+1212-555-1234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		assertThat(this.validator.validate(cardHolder)).isEmpty();
	}

	@Test
	void testValidCardHolderWithoutPhoneNumber() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		assertThat(this.validator.validate(cardHolder)).isEmpty();
	}

	@Test
	void testInvalidFirstNameNull() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName(null)
			.lastName("Doe")
			.email("john.doe@example.com")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidFirstNameEmpty() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("")
			.lastName("Doe")
			.email("john.doe@example.com")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidFirstNameContainsLessThan() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John<Script>")
			.lastName("Doe")
			.email("john.doe@example.com")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
		assertThat(violations.stream().map((v) -> v.getMessage()))
			.anyMatch((msg) -> msg.contains("invalid characters"));
	}

	@Test
	void testInvalidFirstNameContainsAmpersand() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John & Jane")
			.lastName("Doe")
			.email("john.doe@example.com")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidFirstNameExceeds255Characters() {
		String longName = "A".repeat(256);
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName(longName)
			.lastName("Doe")
			.email("john.doe@example.com")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testValidFirstNameAt255Characters() {
		String maxName = "A".repeat(255);
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName(maxName)
			.lastName("Doe")
			.email("john.doe@example.com")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		assertThat(this.validator.validate(cardHolder)).isEmpty();
	}

	@Test
	void testInvalidLastNameNull() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName(null)
			.email("john.doe@example.com")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLastNameContainsBackslash() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe\\Smith")
			.email("john.doe@example.com")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLastNameContainsForwardSlash() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe/Smith")
			.email("john.doe@example.com")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidEmailNull() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email(null)
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidEmailFormat() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("invalid-email-format")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidPhoneNumberMissingCountryCode() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("212-555-1234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidPhoneNumberWithoutPlus() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("1-212-555-1234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidPhoneNumberHyphenAfterCountryCode() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+1-212-555-1234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidPhoneNumberWithParentheses() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+1(212) 555-1234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidPhoneNumberWrongLength() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+1212-555-123")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidPhoneNumberWithSpaces() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+1 212 555 1234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidPhoneNumberWithDots() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+1.212.555.1234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidPhoneNumberWrongCountryCode() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+44-212-555-1234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		var violations = this.validator.validate(cardHolder);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testValidPhoneNumberWithHyphens() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+1212-555-1234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		assertThat(this.validator.validate(cardHolder)).isEmpty();
	}

	@Test
	void testValidPhoneNumberWithoutHyphens() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+12125551234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		assertThat(this.validator.validate(cardHolder)).isEmpty();
	}

	@Test
	void testValidPhoneNumberAlternateFormat() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+1800-555-0123")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		assertThat(this.validator.validate(cardHolder)).isEmpty();
	}

	@Test
	void testValidPhoneNumberWithLeadingOne() {
		CardHolderDto cardHolder = CardHolderDto.builder()
			.firstName("John")
			.lastName("Doe")
			.email("john.doe@example.com")
			.phoneNumber("+17035551234")
			.billingAddress(BillingAddress.builder()
				.line1("123 Main St")
				.city("New York")
				.country("US")
				.postalCode("10001")
				.build())
			.build();

		assertThat(this.validator.validate(cardHolder)).isEmpty();
	}

}
