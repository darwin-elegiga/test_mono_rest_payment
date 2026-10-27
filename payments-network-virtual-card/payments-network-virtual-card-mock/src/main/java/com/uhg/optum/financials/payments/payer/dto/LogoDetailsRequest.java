package com.uhg.optum.financials.payments.payer.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class LogoDetailsRequest {

	@Getter
	@Setter
	@Schema(description = "Account short code", example = "acc123")
	private String accountShortCode;

	@Getter
	@Setter
	@Schema(description = "Sub-account short code", example = "subAcc123")
	private String subAccountShortCode;

	@Getter
	@Setter
	@Schema(description = "Organization short code", example = "org123")
	private String orgShortCode;

}
