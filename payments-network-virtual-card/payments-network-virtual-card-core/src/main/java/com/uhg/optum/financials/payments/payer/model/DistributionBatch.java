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
@Table(name = "distribution_batch", schema = "virtual_card")
@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class DistributionBatch {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "correlation_id", nullable = false, length = 100, unique = true)
	private String correlationId;

	@Column(name = "zip_file_name", length = 255)
	private String zipFileName;

	@Column(name = "status", nullable = false, length = 50)
	private String status;

	@Column(name = "retry_count", columnDefinition = "INT DEFAULT 0")
	private Integer retryCount;

	@Column(name = "error_message", columnDefinition = "TEXT")
	private String errorMessage;

	@Column(name = "created_at", nullable = false, columnDefinition = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP")
	private LocalDateTime createdAt;

	@Column(name = "updated_at", nullable = false, columnDefinition = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP")
	private LocalDateTime updatedAt;

	@Column(name = "sent_at")
	private LocalDateTime sentAt;

	@Column(name = "acknowledge_at")
	private LocalDateTime acknowledgeAt;

}
