package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;

@Entity
@Table(name = "distribution_record", schema = "virtual_card")
@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class DistributionRecord {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "distribution_batch_id", nullable = false)
	private Long distributionBatchId;

	@Column(name = "payment_id", nullable = false, length = 100)
	private String paymentId;

	@Column(name = "card_guid", nullable = false, length = 100)
	private String cardGuid;

	@Column(name = "html_file_name", length = 255)
	private String htmlFileName;

	@Column(name = "distribution_method", length = 20)
	private String distributionMethod;

	@Column(name = "status", length = 50)
	private String status;

	@Column(name = "error_message", columnDefinition = "TEXT")
	private String errorMessage;

	@Column(name = "created_at", nullable = false, columnDefinition = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP")
	private LocalDateTime createdAt;

	@Column(name = "updated_at", nullable = false, columnDefinition = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP")
	private LocalDateTime updatedAt;

}
