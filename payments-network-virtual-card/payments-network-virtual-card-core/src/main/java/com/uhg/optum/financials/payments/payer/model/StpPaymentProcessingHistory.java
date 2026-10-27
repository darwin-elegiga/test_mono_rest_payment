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

import java.time.LocalDateTime;

@Entity
@Getter
@Setter
@Table(name = "stp_payment_processing_history", schema = "virtual_card")

public class StpPaymentProcessingHistory {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "network_payment_id", nullable = false, length = 50)
	private String networkPaymentId;

	@Enumerated(EnumType.STRING)
	@Column(name = "status", nullable = false, length = 50)
	private StpPaymentProcessingStatus status;

	@Column(name = "remarks", length = 1000)
	private String remarks;

	@CreationTimestamp
	@Column(name = "created_at", nullable = false, updatable = false)
	private LocalDateTime createdAt;

}
