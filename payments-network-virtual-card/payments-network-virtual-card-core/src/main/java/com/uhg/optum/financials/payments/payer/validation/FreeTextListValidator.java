package com.uhg.optum.financials.payments.payer.validation;

import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;

import java.util.List;

/**
 * Validator for List of free-text items according to shared validation rules. - Each item
 * max 255 characters - Each item must not contain disallowed characters: &lt; &gt; &amp;
 * &quot; ' \ / and control characters
 *
 * @author Payments Team
 */
public class FreeTextListValidator implements ConstraintValidator<FreeTextListValid, java.util.List<String>> {

	// Disallowed characters: < > & " ' \ /
	private static final String DISALLOWED_CHARS_REGEX = "[<>&\"'\\\\/<\u0000-\u001F]";

	@Override
	public void initialize(FreeTextListValid constraintAnnotation) {
		// No initialization needed
	}

	@Override
	public boolean isValid(List<String> value, ConstraintValidatorContext context) {
		// Null or empty lists are valid
		if (value == null || value.isEmpty()) {
			return true;
		}

		// Validate each item in the list
		for (String item : value) {
			if (item == null) {
				continue; // Null items are allowed, handled by @NotNull if needed
			}

			// Check length (max 255 characters)
			if (item.length() > 255) {
				return false;
			}

			// Check for disallowed characters
			if (item.matches(".*" + DISALLOWED_CHARS_REGEX + ".*")) {
				return false;
			}
		}

		return true;
	}

}
