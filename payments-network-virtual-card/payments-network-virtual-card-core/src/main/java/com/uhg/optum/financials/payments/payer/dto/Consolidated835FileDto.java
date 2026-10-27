package com.uhg.optum.financials.payments.payer.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import lombok.Getter;
import lombok.Setter;
import lombok.ToString;

import java.util.List;

@Getter
@Setter
@ToString
public class Consolidated835FileDto {

	@NotBlank(message = "Origin cannot be null or empty")
	@Schema(description = "Origin of the request", example = "PayerSystem")
	private String origin;

	@NotBlank(message = "Organization Short code cannot be null or empty")
	@Schema(description = "Organization short code", example = "ORG123")
	private String orgShortCode;

	@Valid
	private List<Remit835SplitRequestDto> remit835FileInformation;

}
