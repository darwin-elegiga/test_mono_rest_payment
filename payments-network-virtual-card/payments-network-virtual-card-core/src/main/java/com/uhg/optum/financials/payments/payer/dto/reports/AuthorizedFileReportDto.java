package com.uhg.optum.financials.payments.payer.dto.reports;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.math.BigDecimal;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class AuthorizedFileReportDto {

	private String cardNumber;

	private String transactionDate;

	private String transactionCurrencyCode;

	private String addressVerificationResponse;

	private String authorizationResponse;

	private BigDecimal authorizationAmount;

	private String authorizationCode;

	private String networkCode;

	private String merchantNumber;

	private String merchantName;

	private String merchantCategoryCode;

	private String seExternalIdNumber;

	private String merchantCountryCode;

	private String bin;

	private String tpa;

}
