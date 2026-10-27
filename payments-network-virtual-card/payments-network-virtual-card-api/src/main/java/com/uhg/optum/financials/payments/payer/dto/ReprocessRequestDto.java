package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

/**
 * Exactly one of {@code correlationId} or {@code paymentId} must be provided — the
 * controller validates this before dispatching, since each scopes the reprocess operation
 * differently (every PE payment in a correlation group vs. a single payment).
 *
 * @author Sreekar basetty
 *
 */
@Getter
@Setter
public class ReprocessRequestDto {

	private String correlationId;

	private String paymentId;

}
