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
@Constraint(validatedBy = ValidZipCode.class)
public @interface ZipCodeValidator {

	String message() default "Acquirer Zip Code should be 5 digits Numeric Value";

	Class<?>[] groups() default {};

	Class<? extends Payload>[] payload() default {};

}
