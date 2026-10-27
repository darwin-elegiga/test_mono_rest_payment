package com.uhg.optum.financials.payments.payer.dto.batch;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDateTime;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class BatchDto {

	@Getter
	@Setter
	@Schema(example = "12e12e1", description = "Batch Id")

	private String batchId;

	@Getter
	@Setter
	@Schema(example = "optumpay", description = "network")
	private String network;

	@Getter
	@Setter
	@Schema(example = "1", description = "Number of payments in the batch")

	private Long numberOfPayments;

	@Getter
	@Setter
	@Schema(example = "12000.00", description = "Total Amount")

	private BigDecimal totalAmount;

	@Getter
	@Setter

	@Schema(example = "C", description = "Status of Batch Detail")

	private String status;

	@Getter
	@Setter
	private LocalDateTime createdOn;

	@Getter
	@Setter
	private LocalDateTime updatedOn;

}
