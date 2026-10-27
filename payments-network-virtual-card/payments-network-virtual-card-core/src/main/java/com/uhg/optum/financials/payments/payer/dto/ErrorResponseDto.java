package com.uhg.optum.financials.payments.payer.dto;

import jakarta.validation.constraints.NotBlank;
import lombok.Getter;
import lombok.Setter;

import java.util.LinkedHashMap;

@Getter
@Setter
public class ErrorResponseDto {

	@NotBlank
	private String traceId;

	@NotBlank
	private String errorCode;

	@NotBlank
	private String errorDescription;

	@NotBlank
	private String errorMessage;

	private LinkedHashMap<String, Object> additionalInfo;

}
