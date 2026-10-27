package com.uhg.optum.financials.payments.payer.validation;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import jakarta.validation.Validation;
import jakarta.validation.Validator;
import jakarta.validation.ValidatorFactory;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Unit tests for FreeTextValidator.
 *
 * @author Payments Team
 */
class FreeTextValidatorTests {

	private Validator validator;

	@BeforeEach
	void setUp() {
		ValidatorFactory factory = Validation.buildDefaultValidatorFactory();
		this.validator = factory.getValidator();
	}

	@Test
	void testValidFreeText() {
		TestDto dto = new TestDto("John Doe");
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	@Test
	void testValidFreeTextWithSpecialCharacters() {
		TestDto dto = new TestDto("John-Doe Smith");
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	@Test
	void testValidFreeTextAt255Characters() {
		String maxString = "A".repeat(255);
		TestDto dto = new TestDto(maxString);
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	@Test
	void testInvalidFreeTextExceeds255Characters() {
		String overLimitString = "A".repeat(256);
		TestDto dto = new TestDto(overLimitString);

		var violations = this.validator.validate(dto);
		assertThat(violations).isNotEmpty();
		assertThat(violations.stream().map((v) -> v.getMessage()))
			.anyMatch((msg) -> msg.contains("invalid characters or exceeds 255 characters"));
	}

	@Test
	void testInvalidFreeTextContainsLessThan() {
		TestDto dto = new TestDto("John <Script>");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextContainsGreaterThan() {
		TestDto dto = new TestDto("John > Doe");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextContainsAmpersand() {
		TestDto dto = new TestDto("John & Jane");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextContainsDoubleQuote() {
		TestDto dto = new TestDto("John \"Doe\"");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextContainsSingleQuote() {
		TestDto dto = new TestDto("John's");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextContainsBackslash() {
		TestDto dto = new TestDto("John\\Doe");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextContainsForwardSlash() {
		TestDto dto = new TestDto("John/Doe");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextContainsTab() {
		TestDto dto = new TestDto("John\tDoe");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextContainsNewline() {
		TestDto dto = new TestDto("John\nDoe");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextContainsCarriageReturn() {
		TestDto dto = new TestDto("John\rDoe");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextContainsNullCharacter() {
		TestDto dto = new TestDto("John\u0000Doe");
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testValidNullValue() {
		// Null is valid for @FreeTextValid, handled by @NotNull or @NotBlank
		TestDto dto = new TestDto(null);
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	// Test DTO class for FreeTextValid annotation
	static class TestDto {

		@FreeTextValid
		private String freeText;

		TestDto(String freeText) {
			this.freeText = freeText;
		}

		String getFreeText() {
			return this.freeText;
		}

	}

}
