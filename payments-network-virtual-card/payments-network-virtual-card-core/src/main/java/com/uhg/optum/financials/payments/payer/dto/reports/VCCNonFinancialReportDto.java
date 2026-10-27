package com.uhg.optum.financials.payments.payer.dto.reports;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class VCCNonFinancialReportDto {

	private String maskedPan;

	private String createdDate;

	private String expiryDate;

	private String status;

	private BigDecimal currentBalance;

	private BigDecimal availableBalance;

	private String issuerPaymentId;

	private String orgAccountSubaccount;

}
