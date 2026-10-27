package com.uhg.optum.financials.payments.payer.validation;

import jakarta.validation.Constraint;
import jakarta.validation.Payload;

import java.lang.annotation.Documented;
import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Target({ ElementType.FIELD, ElementType.PARAMETER })
@Retention(RetentionPolicy.RUNTIME)
@Documented
@Constraint(validatedBy = CityCheck.class)
public @interface CityValidator {

	String message() default "Acquirer city should not be empty or null";

	Class<?>[] groups() default {};

	Class<? extends Payload>[] payload() default {};

}
