package com.uhg.optum.financials.payments.payer.validation;

import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;

public class AddressLineOneCheck implements ConstraintValidator<AddressLineOneValidator, String> {

	@Override
	public boolean isValid(String inputType, ConstraintValidatorContext constraintValidatorContext) {
		if (inputType == null) {
			constraintValidatorContext.disableDefaultConstraintViolation();
			constraintValidatorContext
				.buildConstraintViolationWithTemplate(
						"Acquirer Address Line One filed is missing or should not be null")
				.addConstraintViolation();
			return false;
		}
		if (inputType.isEmpty()) {
			constraintValidatorContext.disableDefaultConstraintViolation();
			constraintValidatorContext
				.buildConstraintViolationWithTemplate("Acquirer Address Line One should not be empty")
				.addConstraintViolation();
			return false;
		}
		return true;
	}

}
