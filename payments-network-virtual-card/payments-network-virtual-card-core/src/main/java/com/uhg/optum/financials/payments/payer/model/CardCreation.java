package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import jakarta.persistence.UniqueConstraint;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Table(name = "card_creations", schema = "virtual_card",
		uniqueConstraints = @UniqueConstraint(columnNames = "request_id"))
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class CardCreation {

	@Id
	@Column(name = "id", nullable = false, updatable = false)
	private UUID id;

	@Column(name = "payment_id", nullable = false, length = 32)
	private String paymentId;

	@Column(name = "card_guid")
	private UUID cardGuid;

	@Column(name = "completion_stage", length = 32)
	private String completionStage;

	@Column(name = "bin", nullable = false)
	private Long bin;

	@Column(name = "amount", nullable = false)
	private Long amount;

	@Column(name = "is_single_spend")
	private Boolean isSingleSpend;

	@Column(name = "expiry_year", nullable = false, length = 4)
	private String expiryYear;

	@Column(name = "expiry_month", nullable = false, length = 2)
	private String expiryMonth;

	@Column(name = "expiry_date", nullable = false)
	private LocalDate expiryDate;

	@ManyToOne(fetch = FetchType.EAGER)
	@JoinColumn(name = "cardholder_id", nullable = false)
	private CardHolder cardHolder;

	@ManyToOne(fetch = FetchType.EAGER)
	@JoinColumn(name = "spend_rule_id", referencedColumnName = "id")
	private SpendRules spendRule;

	@Column(name = "status", nullable = false, length = 25)
	private String status;

	@Column(name = "retry_count", nullable = false)
	private Integer retryCount = 0;

	@Column(name = "error_code", length = 50)
	private String errorCode;

	@Column(name = "error_message", length = 255)
	private String errorMessage;

	@Column(name = "created_at", nullable = false)
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 40)
	private String createdBy;

	@Column(name = "updated_at")
	private LocalDateTime updatedAt;

	@Column(name = "updated_by", length = 40)
	private String updatedBy;

	@Column(name = "completed_at")
	private LocalDateTime completedAt;

	@Column(name = "completed_by", length = 40)
	private String completedBy;

}
