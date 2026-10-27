package com.uhg.optum.financials.payments.payer.validation;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import jakarta.validation.Validation;
import jakarta.validation.Validator;
import jakarta.validation.ValidatorFactory;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Unit tests for FreeTextListValidator.
 *
 * @author Payments Team
 */
class FreeTextListValidatorTests {

	private Validator validator;

	@BeforeEach
	void setUp() {
		ValidatorFactory factory = Validation.buildDefaultValidatorFactory();
		this.validator = factory.getValidator();
	}

	@Test
	void testValidFreeTextList() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001", "RS2-0004"));
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	@Test
	void testValidFreeTextListWithSpecialCharacters() {
		TestListDto dto = new TestListDto(Arrays.asList("Rule-001", "Rule_002", "Rule.003"));
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	@Test
	void testValidFreeTextListWithItemAt255Characters() {
		String maxString = "A".repeat(255);
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001", maxString, "RS2-0004"));
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	@Test
	void testValidEmptyList() {
		TestListDto dto = new TestListDto(new ArrayList<>());
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	@Test
	void testValidNullList() {
		TestListDto dto = new TestListDto(null);
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	@Test
	void testInvalidFreeTextListItemExceeds255Characters() {
		String overLimitString = "A".repeat(256);
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001", overLimitString));

		var violations = this.validator.validate(dto);
		assertThat(violations).isNotEmpty();
		assertThat(violations.stream().map((v) -> v.getMessage()))
			.anyMatch((msg) -> msg.contains("invalid characters or exceeds 255 characters"));
	}

	@Test
	void testInvalidFreeTextListItemContainsLessThan() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001", "RS<2>-0004"));
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextListItemContainsGreaterThan() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001", "RS2 > 0004"));
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextListItemContainsAmpersand() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1 & RS2", "RS2-0004"));
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextListItemContainsDoubleQuote() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001", "RS\"2\"-0004"));
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextListItemContainsSingleQuote() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001", "RS2's-Rule"));
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextListItemContainsBackslash() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001", "RS\\2-0004"));
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextListItemContainsForwardSlash() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1/0001", "RS2-0004"));
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextListItemContainsTab() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001", "RS2\t0004"));
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextListItemContainsNewline() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001\nRS2", "RS2-0004"));
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testInvalidFreeTextListMultipleItemsInvalid() {
		TestListDto dto = new TestListDto(Arrays.asList("RS<1>-0001", "RS\"2\"-0004"));
		assertThat(this.validator.validate(dto)).isNotEmpty();
	}

	@Test
	void testValidFreeTextListWithNullItems() {
		// Null items are allowed, handled by @NotNull if needed
		List<String> items = new ArrayList<>();
		items.add("RS1-0001");
		items.add(null);
		items.add("RS2-0004");
		TestListDto dto = new TestListDto(items);
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	@Test
	void testValidSingleItemList() {
		TestListDto dto = new TestListDto(Arrays.asList("RS1-0001"));
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	@Test
	void testValidLargeList() {
		List<String> items = new ArrayList<>();
		for (int i = 0; i < 100; i++) {
			items.add("Rule-" + String.format("%04d", i));
		}
		TestListDto dto = new TestListDto(items);
		assertThat(this.validator.validate(dto)).isEmpty();
	}

	// Test DTO class for FreeTextListValid annotation
	static class TestListDto {

		@FreeTextListValid
		private List<String> freeTextList;

		TestListDto(List<String> freeTextList) {
			this.freeTextList = freeTextList;
		}

		List<String> getFreeTextList() {
			return this.freeTextList;
		}

	}

}
