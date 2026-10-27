package com.uhg.optum.financials.payments.payer.model;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDateTime;

@Entity
@Getter
@Setter
@Table(name = "batch_detail", schema = "virtual_card")
public class BatchDetail {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "org", nullable = false, length = 20)
	@Schema(example = "ORG001", description = "Organization code")
	private String org;

	@Column(name = "correlation_id", nullable = false, length = 32)
	@Schema(example = "CORR1234567890", description = "Correlation ID used to group batch details")

	private String correlationId;

	@Column(name = "batch_id", nullable = false, length = 32)
	@Schema(example = "BATCH20250619", description = "batch identifier")
	private String batchId;

	@Column(name = "number_of_payments", nullable = false)
	@Schema(example = "2", description = "number of payments")
	private Long numberOfPayments;

	@Column(name = "total_amount", nullable = false)
	@Schema(example = "125000.75", description = "Total amount of all payments in the batch")
	private BigDecimal totalAmount;

	@Column(name = "status", nullable = false)
	@Schema(example = "C", description = "Status of the batch")
	private String status;

	@Column(name = "created_on")
	@Schema(example = "2025-06-19T10:15:30", description = "Timestamp when the batch was created")
	private LocalDateTime createdOn;

	@Column(name = "updated_on")
	@Schema(example = "2025-06-19T12:45:00", description = "Timestamp when the batch was last updated")

	private LocalDateTime updatedOn;

}
