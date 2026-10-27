package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.Valid;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ConsentStatesSaveRequestDto {

	@Schema(description = "Final list of states and consentRequired values from UI")
	@Valid
	@JsonProperty("states")
	private List<ConsentStateUpdateDto> states;

}
