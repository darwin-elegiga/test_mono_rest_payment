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
public class ConsentStatesDto {

	@Schema(description = "Two-letter US state code", example = "CA")
	@JsonProperty("stateCode")
	private String stateCode;

	@Schema(description = "Full name of the US state", example = "California")
	@JsonProperty("stateName")
	private String stateName;

	@Schema(description = "Indicates whether consent is required for this state", example = "true")
	@JsonProperty("consentRequired")
	private boolean consentRequired;

}
