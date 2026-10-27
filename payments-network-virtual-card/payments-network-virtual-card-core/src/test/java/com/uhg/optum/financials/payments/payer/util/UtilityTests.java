package com.uhg.optum.financials.payments.payer.util;

import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class UtilityTests {

	@Test
	void sanitizeForLog_shouldReturnNull_whenInputIsNull() {
		assertThat(Utility.sanitizeForLog(null)).isNull();
	}

	@Test
	void sanitizeForLog_shouldReturnUnchanged_whenInputIsClean() {
		assertThat(Utility.sanitizeForLog("abc-123_XYZ")).isEqualTo("abc-123_XYZ");
	}

	@Test
	void sanitizeForLog_shouldReplaceNewline() {
		assertThat(Utility.sanitizeForLog("abc\ndef")).isEqualTo("abc_def");
	}

	@Test
	void sanitizeForLog_shouldReplaceCarriageReturn() {
		assertThat(Utility.sanitizeForLog("abc\rdef")).isEqualTo("abc_def");
	}

	@Test
	void sanitizeForLog_shouldReplaceTab() {
		assertThat(Utility.sanitizeForLog("abc\tdef")).isEqualTo("abc_def");
	}

	@Test
	void sanitizeForLog_shouldReplaceFormFeed() {
		assertThat(Utility.sanitizeForLog("abc\fdef")).isEqualTo("abc_def");
	}

	@Test
	void sanitizeForLog_shouldReplaceMultipleControlChars() {
		assertThat(Utility.sanitizeForLog("line1\r\nline2\ttab")).isEqualTo("line1__line2_tab");
	}

	@Test
	void sanitizeForLog_shouldReturnEmptyString_whenInputIsEmpty() {
		assertThat(Utility.sanitizeForLog("")).isEqualTo("");
	}

	@Test
	void sanitizeForLog_shouldReplaceNullByteAndOtherControlChars() {
		assertThat(Utility.sanitizeForLog("a\u0000b\u001Fc")).isEqualTo("a_b_c");
	}

	@Test
	void isNonEmpty_shouldReturnFalse_whenNull() {
		assertThat(Utility.isNonEmpty(null)).isFalse();
	}

	@Test
	void isNonEmpty_shouldReturnFalse_whenEmpty() {
		assertThat(Utility.isNonEmpty("")).isFalse();
	}

	@Test
	void isNonEmpty_shouldReturnTrue_whenNonEmpty() {
		assertThat(Utility.isNonEmpty("value")).isTrue();
	}

}
