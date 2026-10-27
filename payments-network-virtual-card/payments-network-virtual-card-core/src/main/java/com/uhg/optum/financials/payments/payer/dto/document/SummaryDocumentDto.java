package com.uhg.optum.financials.payments.payer.dto.document;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import lombok.ToString;

import java.io.Serializable;
import java.math.BigDecimal;
import java.util.List;

@Data
@AllArgsConstructor
@NoArgsConstructor
@ToString
public class SummaryDocumentDto implements Serializable {

	@Getter
	@Setter
	private List<String> issuerPaymentIds;

	@Getter
	@Setter
	private String networkPaymentId;

	@Getter
	@Setter
	private BigDecimal consolidatedPaymentAmount;

	@Getter
	@Setter
	private String correlationId;

}
