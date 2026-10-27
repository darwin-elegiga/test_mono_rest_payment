package com.uhg.optum.financials.payments.payer.validation;

import javax.validation.Constraint;
import javax.validation.Payload;
import java.lang.annotation.Documented;
import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Constraint(validatedBy = AccShortCdValidation.class)
@Target({ ElementType.FIELD, ElementType.PARAMETER })
@Retention(RetentionPolicy.RUNTIME)
@Documented
public @interface AccShortCdValidator {

	String message() default "If subAccShortCd is not null, accShortCd must be present";

	Class<?>[] groups() default {};

	Class<? extends Payload>[] payload() default {};

}
