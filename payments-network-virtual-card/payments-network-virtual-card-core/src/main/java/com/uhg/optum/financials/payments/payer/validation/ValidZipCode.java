package com.uhg.optum.financials.payments.payer.validation;

import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;

public class ValidZipCode implements ConstraintValidator<ZipCodeValidator, String> {

	@Override
	public void initialize(ZipCodeValidator constraintAnnotation) {
		// initialize is not required as we are not using any properties from the
		// annotation.
	}

	@Override
	public boolean isValid(String input, ConstraintValidatorContext context) {
		if (input == null) {
			context.disableDefaultConstraintViolation();
			context.buildConstraintViolationWithTemplate("Acquirer ZipCode Field is missing or should not be null")
				.addConstraintViolation();
			return false;
		}
		if (input.isEmpty()) {
			context.disableDefaultConstraintViolation();
			context.buildConstraintViolationWithTemplate("Acquirer ZipCode should not be empty")
				.addConstraintViolation();
			return false;
		}
		return input.matches("\\d{5}");
	}

}
