package com.uhg.optum.financials.payments.payer.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotEmpty;
import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.UUID;

@Getter
@Setter
public class CancelCardDto implements Serializable {

	@NotEmpty(message = "paymentId cannot be null or empty.")
	private String paymentId;

	private String reasonCode;

	@Schema(hidden = true)
	private UUID cardGuid;

	public enum Reason {

		FRAUD_RISK, OFAC_BLOCKED, HOTLISTED, OTHER

	}

}
