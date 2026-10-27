package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import jakarta.persistence.UniqueConstraint;
import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;

@Getter
@Setter
@Entity
@Table(name = "virtual_cards", schema = "virtual_card",
		uniqueConstraints = @UniqueConstraint(columnNames = "card_guid"))
public class VirtualCard {

	/**
	 * Deliberately not auto-generated — this must always equal the {@link CardCreation}
	 * id of the specific attempt that actually succeeded, to satisfy the composite
	 * {@code fk_virtual_cards_request} foreign key on {@code (payment_id, id)}.
	 */
	@Id
	@Column(name = "id", nullable = false, updatable = false)
	private UUID id;

	@Column(name = "card_guid", nullable = false, unique = true)
	private UUID cardGuid;

	@Column(name = "payment_id", nullable = false, length = 32)
	private String paymentId;

	@Column(name = "initial_balance")
	private BigDecimal initialBalance;

	@Column(name = "current_balance")
	private BigDecimal currentBalance;

	@Column(name = "available_balance")
	private BigDecimal availableBalance;

	@Column(name = "currency", length = 3)
	private String currency;

	@Column(name = "expiry_date", nullable = false)
	private LocalDate expiryDate;

	@Column(name = "status", nullable = false, length = 10)
	private String status;

	@Column(name = "spend_rules", nullable = false)
	private List<String> spend_rules;

	@Column(name = "created_at", nullable = false)
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 40)
	private String createdBy;

	@Column(name = "updated_at")
	private LocalDateTime updatedAt;

	@Column(name = "updated_by", length = 40)
	private String updatedBy;

	@Column(name = "masked_pan")
	private String maskedPan;

	@Column(name = "provider_tin", length = 20)
	private String providerTin;

	@Column(name = "provider_state", length = 2)
	private String providerState;

	@Column(name = "activation_status", nullable = false, length = 30)
	private String activationStatus;

	@Column(name = "activation_reason", length = 50)
	private String activationReason;

	@Column(name = "activation_status_updated_at")
	private LocalDateTime activationStatusUpdatedAt;

	public VirtualCard() {
		this.createdAt = LocalDateTime.now();
	}

}
