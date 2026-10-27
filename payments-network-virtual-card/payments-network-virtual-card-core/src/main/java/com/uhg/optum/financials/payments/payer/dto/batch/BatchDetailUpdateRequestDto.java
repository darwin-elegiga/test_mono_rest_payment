package com.uhg.optum.financials.payments.payer.dto.batch;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotEmpty;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.math.BigDecimal;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class BatchDetailUpdateRequestDto {

	@Getter
	@Setter
	@NotEmpty(message = "Status is required")
	@Schema(description = "Status to update", example = "P", allowableValues = { "P", "I", "R", "C", "F", "X" })
	private String status;

	@Getter
	@Setter
	@NotEmpty(message = "CorrelationID is required")
	@Schema(description = "Correlation ID to filter batches", example = "CORR-123456")
	private String correlationId;

	@Getter
	@Setter
	@NotEmpty(message = "BatchID is required")
	@Schema(description = "Batch ID to filter batch", example = "BATCH-123456")
	private String batchId;

	@Getter
	@Setter
	@Schema(description = "Enter Amount To Be Updated", example = "5678.78")
	private BigDecimal amount;

	@Getter
	@Setter
	@Schema(description = "Enter Number Of Payments To Be Updated", example = "100")
	private Long numberOfPayments;

}
