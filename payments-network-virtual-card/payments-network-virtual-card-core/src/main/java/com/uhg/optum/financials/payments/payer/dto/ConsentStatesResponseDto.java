package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ConsentStatesResponseDto {

	@Schema(description = "List of US states with their consent requirement status")
	@JsonProperty("states")
	private List<ConsentStatesDto> states;

	@Schema(description = "Total number of configured U.S. states returned", example = "50")
	@JsonProperty("total")
	private int total;

}
