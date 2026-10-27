package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Getter
@Setter
@Table(name = "card_cancellations", schema = "virtual_card")
public class CardCancellation {

	@Id
	@Column(name = "id", nullable = false, updatable = false)
	private UUID id;

	@Column(name = "payment_id", nullable = false, unique = true, length = 32)
	private String paymentId;

	@Column(name = "card_guid")
	private UUID cardGuid;

	@Column(name = "reason_code")
	private String reasonCode;

	@Column(name = "status")
	private String status;

	@Column(name = "created_at")
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 40)
	private String createdBy;

	@Column(name = "completed_at")
	private LocalDateTime completedAt;

	@Column(name = "completed_by", length = 40)
	private String completedBy;

	@Column(name = "updated_at")
	private LocalDateTime updatedAt;

	@Column(name = "updated_by", length = 40)
	private String updatedBy;

	@Column(name = "error_code")
	private String errorCode;

	@Column(name = "error_message")
	private String errorMessage;

}
