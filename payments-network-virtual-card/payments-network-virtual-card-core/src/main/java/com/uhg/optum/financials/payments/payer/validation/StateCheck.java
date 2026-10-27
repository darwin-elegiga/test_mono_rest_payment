package com.uhg.optum.financials.payments.payer.validation;

import com.uhg.optum.financials.payments.payer.config.StatesConfig;
import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;
import org.springframework.beans.factory.annotation.Autowired;

import java.util.Map;

public class StateCheck implements ConstraintValidator<StateValidator, String> {

	@Autowired
	private StatesConfig statesConfig;

	@Override
	public boolean isValid(String input, ConstraintValidatorContext constraintValidatorContext) {
		if (input == null) {
			constraintValidatorContext.disableDefaultConstraintViolation();
			constraintValidatorContext
				.buildConstraintViolationWithTemplate("Acquirer State filed is missing or should not be null")
				.addConstraintViolation();
			return false;
		}
		if (input.isEmpty()) {
			constraintValidatorContext.disableDefaultConstraintViolation();
			constraintValidatorContext.buildConstraintViolationWithTemplate("Acquirer State should not be empty")
				.addConstraintViolation();
			return false;
		}
		Map<String, String> stateMap = this.statesConfig.getStates();
		return stateMap.keySet().stream().anyMatch((key) -> key.equalsIgnoreCase(input))
				|| stateMap.values().stream().anyMatch((value) -> value.equalsIgnoreCase(input));

	}

}
