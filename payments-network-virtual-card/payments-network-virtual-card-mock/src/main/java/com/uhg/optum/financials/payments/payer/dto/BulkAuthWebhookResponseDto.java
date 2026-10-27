package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

@Getter
@Setter
public class BulkAuthWebhookResponseDto {

	private LocalDateTime startDateTime;

	private LocalDateTime endDateTime;

	private int totalCardsFound;

	private int processedCards;

	private int totalWebhookCalls;

	private List<String> failedCards = new ArrayList<>();

	private List<String> receiptIds = new ArrayList<>();

	private Map<String, List<String>> receiptIdsByCardId = new LinkedHashMap<>();

}
