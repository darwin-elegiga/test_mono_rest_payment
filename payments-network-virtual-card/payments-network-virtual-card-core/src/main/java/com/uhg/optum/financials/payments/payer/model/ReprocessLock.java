package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;

/**
 * Marks a payment ({@code "payment:" + paymentId}) or correlation
 * ({@code "correlation:" + correlationId}) as currently having a reprocess attempt in
 * flight, so a second trigger for the same identifier can be rejected instead of racing
 * the first one off the same stale state.
 *
 * <p>
 * One row persists per identifier ever triggered — {@code status} toggles between
 * {@code AVAILABLE}/{@code IN_PROGRESS} rather than the row being deleted on release, so
 * every trigger/release is kept as a durable audit trail.
 *
 * @author Sreekar basetty
 */
@Entity
@Getter
@Setter
@NoArgsConstructor
@Table(name = "reprocess_lock", schema = "virtual_card")
public class ReprocessLock {

	@Id
	@Column(name = "identifier", nullable = false)
	private String identifier;

	@Column(name = "status", nullable = false, length = 20)
	private String status;

	@Column(name = "triggered_at", nullable = false)
	private LocalDateTime triggeredAt;

	@Column(name = "released_at")
	private LocalDateTime releasedAt;

}
