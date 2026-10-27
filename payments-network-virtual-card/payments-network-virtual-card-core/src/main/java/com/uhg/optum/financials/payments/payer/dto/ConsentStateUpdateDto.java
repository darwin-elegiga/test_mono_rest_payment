package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ConsentStateUpdateDto {

	@Schema(description = "Two-letter US state code", example = "CA")
	@NotBlank(message = "stateCode cannot be blank")
	@Pattern(regexp = "^[A-Za-z]{2}$", message = "stateCode must be a 2-letter code")
	@JsonProperty("stateCode")
	private String stateCode;

	@Schema(description = "Indicates whether consent is required for this state", example = "true")
	@NotNull
	@JsonProperty("consentRequired")
	private Boolean consentRequired;

}
