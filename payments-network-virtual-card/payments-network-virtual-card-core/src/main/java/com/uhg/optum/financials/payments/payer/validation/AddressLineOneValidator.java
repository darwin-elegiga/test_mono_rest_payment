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
@Constraint(validatedBy = AddressLineOneCheck.class)
public @interface AddressLineOneValidator {

	String message() default "Acquirer Address Line One should not be empty or null";

	Class<?>[] groups() default {};

	Class<? extends Payload>[] payload() default {};

}
