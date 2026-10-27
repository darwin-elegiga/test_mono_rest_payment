package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.SequenceGenerator;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Getter
@Setter
@Table(name = "orphan_events", schema = "virtual_card")
public class OrphanEvents {

	@Id
	@GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "virtual_card.orphan_events_id_seq")
	@SequenceGenerator(name = "virtual_card.orphan_events_id_seq", allocationSize = 1)
	@Column(name = "id", nullable = false, updatable = false)
	private Long id;

	@Column(name = "card_id")
	private UUID cardId;

	@Column(name = "receipt_id", length = 20)
	private String receiptId;

	@Column(name = "original_auth_request_id", length = 256)
	private String originalAuthRequestId;

	@Column(name = "payment_id", length = 32)
	private String paymentId;

	@Column(name = "client_request_id")
	private String clientRequestId;

	@Column(name = "event_type")
	private String eventType;

	@JdbcTypeCode(SqlTypes.JSON)
	@Column(columnDefinition = "jsonb", name = "event_payload")
	private String eventPayload;

	@Column(name = "created_at", nullable = false)
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 40)
	private String createdBy;

	@Column(name = "updated_at")
	private LocalDateTime updatedAt;

	@Column(name = "updated_by", length = 40)
	private String updatedBy;

}
