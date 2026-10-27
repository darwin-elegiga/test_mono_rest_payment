package com.uhg.optum.financials.payments.payer.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;
import lombok.ToString;

@Getter
@Setter
@AllArgsConstructor
@ToString
public class Expiry {

	@NotBlank
	@Pattern(regexp = "^(0[1-9]|1[0-2])$", message = "Month must be between 01 and 12")
	private String month;

	@Pattern(regexp = "^\\d{2}$")
	private String year;

}
