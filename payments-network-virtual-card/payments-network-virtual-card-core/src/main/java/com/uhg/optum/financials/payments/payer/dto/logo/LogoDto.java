package com.uhg.optum.financials.payments.payer.dto.logo;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.uhg.optum.financials.payments.organization.events.LogoUpdated;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
@JsonIgnoreProperties(ignoreUnknown = true)
public class LogoDto {

	@Getter
	@Setter
	LogoUpdated logoUpdated;

}
