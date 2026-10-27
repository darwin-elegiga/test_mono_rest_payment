package com.uhg.optum.financials.payments.payer.validation;

import jakarta.validation.Constraint;
import jakarta.validation.Payload;

import java.lang.annotation.Documented;
import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * Validates free-text fields according to shared validation rules. - Max 255 characters -
 * Must not contain disallowed characters: &lt; &gt; &amp; &quot; ' \ / and control
 * characters
 *
 * @author Payments Team
 */
@Target({ ElementType.METHOD, ElementType.FIELD, ElementType.PARAMETER })
@Retention(RetentionPolicy.RUNTIME)
@Constraint(validatedBy = FreeTextValidator.class)
@Documented
public @interface FreeTextValid {

	String message() default "Field contains invalid characters or exceeds 255 characters. "
			+ "Disallowed characters: < > & \" ' \\ / and control characters (U+0000 to U+001F)";

	Class<?>[] groups() default {};

	Class<? extends Payload>[] payload() default {};

}
