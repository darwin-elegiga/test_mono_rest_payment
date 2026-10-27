package com.uhg.optum.financials.payments.payer.validation;

import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;

public class CityCheck implements ConstraintValidator<CityValidator, String> {

	@Override
	public boolean isValid(String input, ConstraintValidatorContext context) {
		if (input == null) {
			context.disableDefaultConstraintViolation();
			context.buildConstraintViolationWithTemplate("Acquirer City filed is missing or should not be null")
				.addConstraintViolation();
			return false;
		}
		if (input.isEmpty()) {
			context.disableDefaultConstraintViolation();
			context.buildConstraintViolationWithTemplate("Acquirer City should not be empty").addConstraintViolation();
			return false;
		}
		return true;
	}

}
