package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;

@Getter
@Setter
public class BulkClearFileResponseDto {

	private LocalDateTime startDateTime;

	private LocalDateTime endDateTime;

	private int totalCardsFound;

	private int totalClearRecords;

	private int totalDebitTransactions;

	private int totalCreditTransactions;

	private String fileName;

}
