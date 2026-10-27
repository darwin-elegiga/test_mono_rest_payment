package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;

@Entity
@Getter
@Setter
@Table(name = "correlation_mapping", schema = "virtual_card")
public class CorrelationMapping {

	@Column(name = "actual_correlation_id", nullable = false, length = 32)
	private String actualCorrelationId;

	@Id
	@Column(name = "internal_correlation_id", nullable = false, length = 32)
	private String internalCorrelationId;

	@Column(name = "created_on", nullable = false)
	private LocalDateTime createdOn;

}
