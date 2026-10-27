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
class ValidZipCodeTests {

	@InjectMocks
	ValidZipCode validZipCode;

	@Mock
	ConstraintValidatorContext constraintValidatorContext;

	@Mock
	ZipCodeValidator zipCodeValidator;

	@Test
	void testInitialize() {
		this.validZipCode.initialize(this.zipCodeValidator);
	}

	@Test
	void testIsValidEmptyString() {

		String inputType = "";
		HibernateConstraintViolationBuilder builder = Mockito.mock(HibernateConstraintViolationBuilder.class);
		Mockito.when(this.constraintValidatorContext.buildConstraintViolationWithTemplate(Mockito.anyString()))
			.thenReturn(builder);
		Mockito.when(builder.addConstraintViolation()).thenReturn(this.constraintValidatorContext);
		boolean result = this.validZipCode.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isFalse();
	}

	@Test
	void testIsValidForNull() {

		String inputType = null;
		HibernateConstraintViolationBuilder builder = Mockito.mock(HibernateConstraintViolationBuilder.class);
		Mockito.when(this.constraintValidatorContext.buildConstraintViolationWithTemplate(Mockito.anyString()))
			.thenReturn(builder);
		Mockito.when(builder.addConstraintViolation()).thenReturn(this.constraintValidatorContext);
		boolean result = this.validZipCode.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isFalse();
	}

	@Test
	void testIsValid() {

		String inputType = "12345";
		boolean result = this.validZipCode.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isTrue();
	}

}
