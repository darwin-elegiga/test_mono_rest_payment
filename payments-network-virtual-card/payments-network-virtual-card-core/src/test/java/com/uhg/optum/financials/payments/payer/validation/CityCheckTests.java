package com.uhg.optum.financials.payments.payer.validation;

import jakarta.validation.ConstraintValidatorContext;
import org.assertj.core.api.Assertions;
import org.hibernate.validator.constraintvalidation.HibernateConstraintViolationBuilder;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class CityCheckTests {

	@InjectMocks
	CityCheck cityCheck;

	@Mock
	ConstraintValidatorContext constraintValidatorContext;

	@Test
	void testIsValidEmptyString() {

		String inputType = "";
		HibernateConstraintViolationBuilder builder = Mockito.mock(HibernateConstraintViolationBuilder.class);
		Mockito.when(this.constraintValidatorContext.buildConstraintViolationWithTemplate(Mockito.anyString()))
			.thenReturn(builder);
		Mockito.when(builder.addConstraintViolation()).thenReturn(this.constraintValidatorContext);
		boolean result = this.cityCheck.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isFalse();
	}

	@Test
	void testIsValidForNull() {

		String inputType = null;
		HibernateConstraintViolationBuilder builder = Mockito.mock(HibernateConstraintViolationBuilder.class);
		Mockito.when(this.constraintValidatorContext.buildConstraintViolationWithTemplate(Mockito.anyString()))
			.thenReturn(builder);
		Mockito.when(builder.addConstraintViolation()).thenReturn(this.constraintValidatorContext);
		boolean result = this.cityCheck.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isFalse();
	}

	@Test
	void testIsValidForValidInput() {

		String inputType = "1234567890";
		Assertions.assertThat(this.cityCheck.isValid(inputType, this.constraintValidatorContext)).isTrue();
	}

}
