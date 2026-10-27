package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.payments.payer.enums.StpPaymentProcessingStatus;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDateTime;

@Entity
@Getter
@Setter
@Table(name = "stp_payment_processing", schema = "virtual_card")
public class StpPaymentProcessing {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "payment_request_id", nullable = false)
	private Long paymentRequestId;

	@Column(name = "network_partner", nullable = false, length = 100)
	private String networkPartner;

	@Column(name = "network_payment_id", nullable = false, length = 50, unique = true)
	private String networkPaymentId;

	@Enumerated(EnumType.STRING)
	@Column(name = "status", nullable = false, length = 50)
	private StpPaymentProcessingStatus status;

	@Column(name = "retry_count", nullable = false)
	private Integer retryCount;

	@Column(name = "rejection_code", length = 20)
	private String rejectionCode;

	@Column(name = "rejection_reason", length = 500)
	private String rejectionReason;

	@CreationTimestamp
	@Column(name = "created_at", nullable = false, updatable = false)
	private LocalDateTime createdAt;

	@UpdateTimestamp
	@Column(name = "updated_at", nullable = false)
	private LocalDateTime updatedAt;

}
