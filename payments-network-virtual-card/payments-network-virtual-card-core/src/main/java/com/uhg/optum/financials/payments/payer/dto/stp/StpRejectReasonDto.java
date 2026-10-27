package com.uhg.optum.financials.payments.payer.dto.stp;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.io.Serializable;

@Data
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class StpRejectReasonDto implements Serializable {

	@NotBlank(message = "Network Partner is required")
	private String networkPartner;

	@NotNull(message = "Optum TransactionId is required")
	private String optumTransactionId;

	@NotBlank(message = "Rejection ReasonCode is required")
	private String rejectionReasonCode;

	@NotBlank(message = "Rejection Reason is required")
	private String rejectionReason;

}
