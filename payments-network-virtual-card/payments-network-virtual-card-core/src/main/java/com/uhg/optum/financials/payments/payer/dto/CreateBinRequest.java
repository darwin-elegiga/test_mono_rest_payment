package com.uhg.optum.financials.payments.payer.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Positive;
import jakarta.validation.constraints.Size;
import lombok.Data;

@Data
public class CreateBinRequest {

	@NotNull
	@Positive
	private Long bin;

	@Size(max = 50)
	@NotBlank
	private String fundingType;

}
