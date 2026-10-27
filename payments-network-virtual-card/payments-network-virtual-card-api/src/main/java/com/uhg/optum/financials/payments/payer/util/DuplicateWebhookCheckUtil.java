package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.repository.ProcessedWebhookRepository;
import org.springframework.stereotype.Component;

@Component
public class DuplicateWebhookCheckUtil {

	private final ProcessedWebhookRepository processedWebhookRepository;

	public DuplicateWebhookCheckUtil(ProcessedWebhookRepository processedWebhookRepository) {
		this.processedWebhookRepository = processedWebhookRepository;
	}

	public int checkForProcessedWebhook(String id, String type, String processedStatus) {
		return this.processedWebhookRepository.tryInsertProcessed(id, type, processedStatus);
	}

}
