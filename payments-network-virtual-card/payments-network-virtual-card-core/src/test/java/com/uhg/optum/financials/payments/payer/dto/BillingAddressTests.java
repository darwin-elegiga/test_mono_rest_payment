package com.uhg.optum.financials.payments.payer.dto;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import jakarta.validation.Validation;
import jakarta.validation.Validator;
import jakarta.validation.ValidatorFactory;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Unit tests for BillingAddress DTO validation.
 *
 * @author Payments Team
 */
class BillingAddressTests {

	private Validator validator;

	@BeforeEach
	void setUp() {
		ValidatorFactory factory = Validation.buildDefaultValidatorFactory();
		this.validator = factory.getValidator();
	}

	@Test
	void testValidBillingAddress() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		assertThat(this.validator.validate(address)).isEmpty();
	}

	@Test
	void testValidBillingAddressWithApartmentNumber() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street, Apt 4B")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		assertThat(this.validator.validate(address)).isEmpty();
	}

	@Test
	void testInvalidLine1Null() {
		BillingAddress address = BillingAddress.builder()
			.line1(null)
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLine1Empty() {
		BillingAddress address = BillingAddress.builder()
			.line1("")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLine1ContainsLessThan() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 <Main> Street")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLine1ContainsGreaterThan() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main > Street")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLine1ContainsAmpersand() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main & Oak Street")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLine1ContainsDoubleQuote() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 \"Main\" Street")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLine1ContainsSingleQuote() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main's Street")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLine1ContainsBackslash() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main\\Street")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLine1ContainsForwardSlash() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main/Street")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidLine1Exceeds255Characters() {
		String longLine = "A".repeat(256);
		BillingAddress address = BillingAddress.builder()
			.line1(longLine)
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testValidLine1At255Characters() {
		String maxLine = "A".repeat(255);
		BillingAddress address = BillingAddress.builder()
			.line1(maxLine)
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		assertThat(this.validator.validate(address)).isEmpty();
	}

	@Test
	void testInvalidCityNull() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city(null)
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidCityEmpty() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidCityContainsAmpersand() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("New & York")
			.country("US")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testValidCityAt255Characters() {
		String maxCity = "A".repeat(255);
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city(maxCity)
			.country("US")
			.postalCode("10001")
			.build();

		assertThat(this.validator.validate(address)).isEmpty();
	}

	@Test
	void testInvalidCountryNotTwoCharacters() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("New York")
			.country("USA")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidCountryLowercase() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("New York")
			.country("us")
			.postalCode("10001")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testValidCountryUS() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("New York")
			.country("US")
			.postalCode("10001")
			.build();

		assertThat(this.validator.validate(address)).isEmpty();
	}

	@Test
	void testValidCountryGB() {
		BillingAddress address = BillingAddress.builder()
			.line1("10 Downing St")
			.city("London")
			.country("GB")
			.postalCode("SW1A 1AA")
			.build();

		assertThat(this.validator.validate(address)).isEmpty();
	}

	@Test
	void testInvalidPostalCodeNull() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("New York")
			.country("US")
			.postalCode(null)
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidPostalCodeEmpty() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("New York")
			.country("US")
			.postalCode("")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testInvalidPostalCodeContainsForwardSlash() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("New York")
			.country("US")
			.postalCode("10001/5678")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

	@Test
	void testValidPostalCodeWithSpace() {
		BillingAddress address = BillingAddress.builder()
			.line1("10 Downing St")
			.city("London")
			.country("GB")
			.postalCode("SW1A 1AA")
			.build();

		assertThat(this.validator.validate(address)).isEmpty();
	}

	@Test
	void testValidPostalCodeAt255Characters() {
		String maxPostalCode = "A".repeat(255);
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("New York")
			.country("US")
			.postalCode(maxPostalCode)
			.build();

		assertThat(this.validator.validate(address)).isEmpty();
	}

	@Test
	void testInvalidPostalCodeContainsControlCharacter() {
		BillingAddress address = BillingAddress.builder()
			.line1("123 Main Street")
			.city("New York")
			.country("US")
			.postalCode("10001\t5678")
			.build();

		var violations = this.validator.validate(address);
		assertThat(violations).isNotEmpty();
	}

}
