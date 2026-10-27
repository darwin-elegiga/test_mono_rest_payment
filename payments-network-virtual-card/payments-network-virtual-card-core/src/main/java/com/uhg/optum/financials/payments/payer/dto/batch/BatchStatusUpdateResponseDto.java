package com.uhg.optum.financials.payments.payer.dto.batch;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class BatchStatusUpdateResponseDto {

	@Getter
	@Setter
	private int updatedCount;

	@Getter
	@Setter
	private String message;

}
