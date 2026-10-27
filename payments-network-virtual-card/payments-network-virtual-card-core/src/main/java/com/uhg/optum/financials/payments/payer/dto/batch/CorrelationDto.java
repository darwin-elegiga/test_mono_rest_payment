package com.uhg.optum.financials.payments.payer.dto.batch;

import com.uhg.optum.financials.shared.client.events.IEvent;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class CorrelationDto implements IEvent {

	@Getter
	@Setter
	@Schema(description = "Org ShortCode", example = "ORG01")
	private String org;

	@Getter
	@Setter
	@Schema(description = "correlationId", example = "123e4567-e89b-12d3-a456-426614174000")
	private String correlationId;

	@Getter
	@Setter
	@Schema(description = "Number of Batches", example = "1")
	private int numberOfBatches;

	@Getter
	@Setter
	private List<BatchDetailDto> batches;

	@Getter
	@Setter
	@Schema(description = "Type", example = "batch_correlated")
	private String type;

	@Override
	public String type() {
		return this.type;
	}

}
