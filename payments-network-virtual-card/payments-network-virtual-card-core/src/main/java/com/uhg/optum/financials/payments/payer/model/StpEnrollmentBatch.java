package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;

@Entity
@Table(name = "stp_enrollment_batch", schema = "virtual_card")
@Getter
@Setter
@NoArgsConstructor
public class StpEnrollmentBatch {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "correlation_id", nullable = false, length = 100, unique = true)
	private String correlationId;

	@Column(name = "network_partner", length = 100)
	private String networkPartner;

	@Column(name = "enrollment_count")
	private Integer enrollmentCount;

	@Column(name = "valid_count")
	private Integer validCount;

	@Column(name = "error_count")
	private Integer errorCount;

	@Column(name = "status", nullable = false, length = 50)
	private String status;

	@Column(name = "callback_status", length = 50)
	private String callbackStatus;

	@Column(name = "preferencing_status", length = 50)
	private String preferencingStatus;

	@Column(name = "created_at", nullable = false, columnDefinition = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP")
	private LocalDateTime createdAt;

	@Column(name = "updated_at", nullable = false, columnDefinition = "TIMESTAMP DEFAULT CURRENT_TIMESTAMP")
	private LocalDateTime updatedAt;

}
