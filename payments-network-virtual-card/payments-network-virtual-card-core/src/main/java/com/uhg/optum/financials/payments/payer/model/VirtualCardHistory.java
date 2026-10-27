package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Table(name = "virtual_card_history", schema = "virtual_card")
@Getter
@Setter
public class VirtualCardHistory {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "card_guid", nullable = false)
	private UUID cardGuid;

	@Column(name = "payment_id", length = 32, nullable = false)
	private String paymentId;

	@Column(length = 25)
	private String status;

	@Column(name = "receipt_id", length = 20)
	private String receiptId;

	@Column(columnDefinition = "TEXT")
	private String reason;

	@Column(length = 3)
	private String currency;

	@Column(name = "initial_balance")
	private BigDecimal initialBalance;

	@Column(name = "available_balance")
	private BigDecimal availableBalance;

	@Column(name = "current_balance")
	private BigDecimal currentBalance;

	@Column(name = "created_at", nullable = false, updatable = false)
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 100)
	private String createdBy;

	@Column(name = "masked_pan")
	private String maskedPan;

	@ManyToOne
	@JoinColumn(name = "card_guid", referencedColumnName = "card_guid", insertable = false, updatable = false)
	private VirtualCard virtualCard;

}
