package com.uhg.optum.financials.payments.payer.validation;

import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;

/**
 * Validator for free-text fields according to shared validation rules. - Max 255
 * characters - Must not contain disallowed characters: &lt; &gt; &amp; &quot; ' \ / and
 * control characters
 *
 * @author Payments Team
 */
public class FreeTextValidator implements ConstraintValidator<FreeTextValid, String> {

	// Disallowed characters: < > & " ' \ /
	private static final String DISALLOWED_CHARS_REGEX = "[<>&\"'\\\\/<\u0000-\u001F]";

	@Override
	public void initialize(FreeTextValid constraintAnnotation) {
		// No initialization needed
	}

	@Override
	public boolean isValid(String value, ConstraintValidatorContext context) {
		// Null values are handled by @NotBlank or @NotNull annotations
		if (value == null) {
			return true;
		}

		// Check length (max 255 characters)
		if (value.length() > 255) {
			return false;
		}

		// Check for disallowed characters
		return !value.matches(".*" + DISALLOWED_CHARS_REGEX + ".*");
	}

}
