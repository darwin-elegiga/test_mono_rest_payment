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
public class AddressLineOneCheckTests {

	@InjectMocks
	AddressLineOneCheck addressLineOneCheck;

	@Mock
	ConstraintValidatorContext constraintValidatorContext;

	@Test
	public void testIsValidEmptyString() {

		String inputType = "";
		HibernateConstraintViolationBuilder builder = Mockito.mock(HibernateConstraintViolationBuilder.class);
		Mockito.when(this.constraintValidatorContext.buildConstraintViolationWithTemplate(Mockito.anyString()))
			.thenReturn(builder);
		Mockito.when(builder.addConstraintViolation()).thenReturn(this.constraintValidatorContext);
		boolean result = this.addressLineOneCheck.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isFalse();
	}

	@Test
	public void testIsValidForNull() {

		String inputType = null;
		HibernateConstraintViolationBuilder builder = Mockito.mock(HibernateConstraintViolationBuilder.class);
		Mockito.when(this.constraintValidatorContext.buildConstraintViolationWithTemplate(Mockito.anyString()))
			.thenReturn(builder);
		Mockito.when(builder.addConstraintViolation()).thenReturn(this.constraintValidatorContext);
		boolean result = this.addressLineOneCheck.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isFalse();
	}

	@Test
	public void testIsValidForValidInput() {

		String inputType = "1234567890";
		Assertions.assertThat(this.addressLineOneCheck.isValid(inputType, this.constraintValidatorContext)).isTrue();
	}

}
