package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.IdClass;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;

@Entity
@Table(name = "processed_webhooks", schema = "virtual_card")
@IdClass(ProcessedWebhookDetailsId.class)
@Getter
@Setter
public class ProcessedWebhookDetails {

	@Id
	@Column(length = 40)
	private String id;

	@Id
	@Column(name = "event_type", length = 60, nullable = false)
	private String eventType;

	@Column(name = "processed_at")
	private LocalDateTime processedAt;

	@Id
	@Column(name = "processed_status", length = 20, nullable = false)
	private String processedStatus;

}
