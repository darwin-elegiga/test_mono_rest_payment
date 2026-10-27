package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;

@Getter
@Setter
public class BulkAuthFileResponseDto {

	private LocalDateTime startDateTime;

	private LocalDateTime endDateTime;

	private int totalCardsFound;

	private int totalAuthRecords;

	private String fileName;

}
