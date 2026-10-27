package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.BulkAuthFileResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkAuthWebhookResponseDto;
import com.uhg.optum.financials.payments.payer.dto.BulkClearFileResponseDto;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;

import java.io.IOException;
import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;
import java.util.UUID;

public interface MockZetaApiCardService {

	CardDetailsDTO getCardDetails(UUID cardI);

	String generateAuthExtract(String cardId, BigDecimal amount) throws IOException;

	AuthExtract getAuthExtractRecord(String receiptId);

	String uploadGenerateAuthExtract(String receiptId, String cardId) throws IOException;

	String generateClearExtract(String receiptId, String settlementIndicator, String cardId) throws IOException;

	BulkAuthWebhookResponseDto triggerAuthWebhooksFromDateTime(LocalDateTime startDateTime, int numberOfAuthEvents);

	BulkAuthWebhookResponseDto triggerAuthWebhooksFromDateTime(LocalDateTime startDateTime, int numberOfAuthEvents,
			List<UUID> cguids);

	BulkAuthFileResponseDto generateAuthFileFromDb(LocalDateTime startDateTime) throws IOException;

	BulkClearFileResponseDto generateClearFileFromDb(LocalDateTime startDateTime) throws IOException;

}
