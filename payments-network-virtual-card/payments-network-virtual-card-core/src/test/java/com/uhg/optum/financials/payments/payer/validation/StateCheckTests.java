package com.uhg.optum.financials.payments.payer.validation;

import com.uhg.optum.financials.payments.payer.config.StatesConfig;
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
class StateCheckTests {

	@InjectMocks
	StateCheck stateCheck;

	@Mock
	ConstraintValidatorContext constraintValidatorContext;

	@Mock
	StatesConfig statesConfig;

	@Test
	void testIsValidEmptyString() {

		String inputType = "";
		HibernateConstraintViolationBuilder builder = Mockito.mock(HibernateConstraintViolationBuilder.class);
		Mockito.when(this.constraintValidatorContext.buildConstraintViolationWithTemplate(Mockito.anyString()))
			.thenReturn(builder);
		Mockito.when(builder.addConstraintViolation()).thenReturn(this.constraintValidatorContext);
		boolean result = this.stateCheck.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isFalse();
	}

	@Test
	void testIsValidForNull() {

		String inputType = null;
		HibernateConstraintViolationBuilder builder = Mockito.mock(HibernateConstraintViolationBuilder.class);
		Mockito.when(this.constraintValidatorContext.buildConstraintViolationWithTemplate(Mockito.anyString()))
			.thenReturn(builder);
		Mockito.when(builder.addConstraintViolation()).thenReturn(this.constraintValidatorContext);
		boolean result = this.stateCheck.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isFalse();
	}

	@Test
	void testIsValidForValidState() {

		String inputType = "NY";
		HibernateConstraintViolationBuilder builder = Mockito.mock(HibernateConstraintViolationBuilder.class);
		boolean result = this.stateCheck.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isFalse();
	}

	@Test
	void testIsValidForValidStateValue() {

		String inputType = "New York";
		HibernateConstraintViolationBuilder builder = Mockito.mock(HibernateConstraintViolationBuilder.class);
		boolean result = this.stateCheck.isValid(inputType, this.constraintValidatorContext);
		Assertions.assertThat(result).isFalse();
	}

}
