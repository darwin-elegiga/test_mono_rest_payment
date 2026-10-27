package com.uhg.optum.financials.payments.payer.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;
import lombok.ToString;
import com.uhg.optum.financials.payments.payer.validation.FreeTextValid;

@Getter
@Setter
@Builder
@ToString
public class BillingAddress {

	@NotBlank
	@FreeTextValid
	private String line1;

	@NotBlank
	@FreeTextValid
	private String city;

	@Pattern(regexp = "^[A-Z]{2}$", message = "Country must be exactly 2 characters long in upper case")
	private String country;

	@NotBlank
	@FreeTextValid
	private String postalCode;

}
