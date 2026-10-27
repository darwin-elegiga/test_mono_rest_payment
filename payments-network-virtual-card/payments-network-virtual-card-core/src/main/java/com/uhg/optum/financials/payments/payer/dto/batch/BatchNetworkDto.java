package com.uhg.optum.financials.payments.payer.dto.batch;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.io.Serializable;
import java.math.BigDecimal;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class BatchNetworkDto implements Serializable {

	@Getter
	@Setter
	@Schema(description = "Network", example = "OPTUMPAY")
	private String network;

	@Getter
	@Setter
	@Schema(description = "Number of Payments", example = "100")
	private Long numberOfPayments;

	@Getter
	@Setter
	@Schema(description = "Total Amount", example = "10000.00")
	private BigDecimal totalAmount;

}
