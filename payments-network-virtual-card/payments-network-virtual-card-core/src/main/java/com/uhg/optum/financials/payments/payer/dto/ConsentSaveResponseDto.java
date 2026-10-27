package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ConsentSaveResponseDto {

	@Schema(description = "Result message", example = "Consent-required states updated successfully")
	@JsonProperty("message")
	private String message;

}
