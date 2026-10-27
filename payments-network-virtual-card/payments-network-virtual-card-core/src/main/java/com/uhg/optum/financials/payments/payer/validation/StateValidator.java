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
@Constraint(validatedBy = StateCheck.class)
public @interface StateValidator {

	String message() default "Acquirer State is invalid";

	Class<?>[] groups() default {};

	Class<? extends Payload>[] payload() default {};

}
