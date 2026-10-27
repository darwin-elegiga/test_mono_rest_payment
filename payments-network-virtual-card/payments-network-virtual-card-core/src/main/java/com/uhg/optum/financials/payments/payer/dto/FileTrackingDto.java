package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
@JsonInclude(JsonInclude.Include.ALWAYS)
public class FileTrackingDto {

	@Getter
	@Setter
	@JsonProperty("835")
	@Schema(name = "835", description = "835 File", example = "outbound/20250513/org/835/org_20250513_06")
	private String _835;

	@Getter
	@Setter
	@Schema(description = "companion File",
			example = "processed/companion/b89ddd48ad8117cd06b984a1539a45e6_companionFile.json")
	private String companion;

	@Getter
	@Setter
	@Schema(description = "Companion file processing status", example = "SUCCESS")
	private String companionStatus;

	@Getter
	@Setter
	@Schema(description = "Payment activity trigger status", example = "SUCCESS")
	private String paymentActivityStatus;

	@Getter
	@Setter
	@Schema(description = "correlation Id", example = "AKJHF87G674J5")
	private String correlationId;

}
