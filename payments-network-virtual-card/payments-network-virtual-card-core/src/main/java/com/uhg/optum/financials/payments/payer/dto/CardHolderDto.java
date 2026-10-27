package com.uhg.optum.financials.payments.payer.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;
import lombok.ToString;
import com.uhg.optum.financials.payments.payer.validation.FreeTextValid;

@Getter
@Setter
@Builder
@ToString
public class CardHolderDto {

	@NotBlank
	@FreeTextValid
	private String firstName;

	@NotBlank
	@FreeTextValid
	private String lastName;

	@NotBlank
	@Email(message = "Invalid email format", regexp = "^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$")
	private String email;

	// US phone number format: +1DDD-DDD-DDDD (with hyphens) or +1XXXXXXXXXX (10
	// consecutive digits)
	// Accepts: +1212-555-1234 or +12125551234
	// Rejects: +1-212-555-1234 (hyphen after +1), 212-555-1234 (missing +1), (212)
	// 555-1234 (parentheses)
	@Pattern(regexp = "^\\+1(?:\\d{3}-\\d{3}-\\d{4}|\\d{10})$",
			message = "Invalid US phone number format. Use +1DDD-DDD-DDDD or +1XXXXXXXXXX")
	private String phoneNumber;

	@Valid
	private BillingAddress billingAddress;

}
