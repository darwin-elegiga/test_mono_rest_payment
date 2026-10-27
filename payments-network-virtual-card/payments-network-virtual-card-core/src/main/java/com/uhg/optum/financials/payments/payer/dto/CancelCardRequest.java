package com.uhg.optum.financials.payments.payer.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotNull;
import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.UUID;

@Getter
@Setter
public class CancelCardRequest implements Serializable {

	@NotNull(message = "requestId cannot be null.")
	private UUID requestId;

	@NotNull(message = "reason cannot be null.")
	private String reasonCode;

	@Schema(hidden = true)
	private UUID cardGuid;

}
