package com.uhg.optum.financials.payments.payer.dto;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.math.BigDecimal;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class PostedFileDto {

	private String cardNumber;

	private String transactionDate;

	private String transactionCode;

	private BigDecimal transactionAmount;

	private String transactionAmountSign; // decide on basic of settlement indicator

	private String authorizationCode;

	private String postDate;

	private String merchantNumber; // MID + TID concatenated

	private String merchantName;

	private String merchantCategoryCode;

	private String seExternalIdNumber; // issuerPaymentId

	private String merchantCountryCode;

	private String bin;

	private String tpa;

}
