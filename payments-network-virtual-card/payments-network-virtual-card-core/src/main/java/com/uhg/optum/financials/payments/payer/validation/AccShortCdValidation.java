package com.uhg.optum.financials.payments.payer.validation;

import com.uhg.optum.financials.payments.payer.dto.SpendRulesDTO;

import javax.validation.ConstraintValidator;
import javax.validation.ConstraintValidatorContext;

public class AccShortCdValidation implements ConstraintValidator<AccShortCdValidator, SpendRulesDTO> {

	@Override
	public void initialize(AccShortCdValidator accShortCdValidator) {
	}

	@Override
	public boolean isValid(SpendRulesDTO dto, ConstraintValidatorContext context) {
		if (dto.getAccounts() != null
				&& (dto.getAccounts().getFirst() == null || dto.getAccounts().getFirst().getAccShortCd().isEmpty())) {
			context.disableDefaultConstraintViolation();
			context.buildConstraintViolationWithTemplate("Account Short Code is required when accounts are provided")
				.addPropertyNode("accounts")
				.addConstraintViolation();
			return false;
		}
		return true;
	}

}
// This class implements the validation logic for the account short code in the context of
// spend rules configuration.
