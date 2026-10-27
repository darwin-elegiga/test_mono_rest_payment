package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.payer.validation.FreeTextValid;
import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotEmpty;
import lombok.Data;

import java.io.Serializable;
import java.util.UUID;

@Data
public class ResetCardRequest implements Serializable {

	@NotEmpty(message = "receiptId cannot be null or empty.")
	@FreeTextValid
	private String receiptId;

	@NotEmpty(message = "reason cannot be null or empty.")
	@FreeTextValid
	private String resetPurpose;

	@NotEmpty(message = "paymentId cannot be null or empty.")
	private String paymentId;

	@Schema(hidden = true)
	private UUID cardGuid;

}
