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
import java.util.UUID;

@Entity
@Table(name = "virtual_card_audit_log", schema = "virtual_card")
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class AuditLog {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "payment_id", nullable = false, length = 32)
	private String paymentId;

	@Column(name = "card_guid")
	private UUID cardGuid;

	@Column(name = "request_id")
	private UUID requestId;

	@Column(name = "receipt_id")
	private String receiptId;

	@Column(name = "event_type", nullable = false, length = 16)
	private String eventType;

	@Column(name = "status", nullable = false, length = 32)
	private String status;

	@Column(name = "source", nullable = false, length = 64)
	private String source;

	@Column(name = "created_at", nullable = false)
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 64)
	private String createdBy;

}
