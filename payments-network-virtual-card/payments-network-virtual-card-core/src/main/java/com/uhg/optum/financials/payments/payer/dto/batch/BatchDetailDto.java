package com.uhg.optum.financials.payments.payer.dto.batch;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.io.Serializable;
import java.util.List;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class BatchDetailDto implements Serializable {

	@Getter
	@Setter
	@Schema(description = "Unique identifier for the batch", example = "1234567")
	private String batchId;

	@Getter
	@Setter
	private List<BatchNetworkDto> networks;

}
