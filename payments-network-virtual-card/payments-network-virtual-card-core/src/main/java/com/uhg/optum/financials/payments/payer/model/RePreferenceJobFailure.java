package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;

@Entity
@Getter
@Setter
@Table(name = "re_preference_job_failure", schema = "virtual_card")
public class RePreferenceJobFailure {

	@Id
	@Column(name = "payment_request_id", nullable = false)
	private Long paymentRequestId;

	@Column(name = "attempt_count", nullable = false)
	private Integer attemptCount;

	@Column(name = "last_failed_on", nullable = false)
	private LocalDateTime lastFailedOn;

	@Column(name = "last_error", columnDefinition = "TEXT")
	private String lastError;

}
