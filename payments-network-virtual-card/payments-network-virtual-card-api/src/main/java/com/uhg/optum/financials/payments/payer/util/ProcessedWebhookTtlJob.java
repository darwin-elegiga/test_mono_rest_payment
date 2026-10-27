package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.repository.ProcessedWebhookRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.INFO;

@Component
@Slf4j
public class ProcessedWebhookTtlJob {

	private final ProcessedWebhookRepository processedWebhookRepository;

	private final LogUtility logUtility;

	public ProcessedWebhookTtlJob(ProcessedWebhookRepository repo, LogUtility logUtility) {
		this.processedWebhookRepository = repo;
		this.logUtility = logUtility;
	}

	@Scheduled(cron = "0 0 */6 * * *")
	@Transactional
	public void cleanupOlderThanSixHours() {
		int deleted = this.processedWebhookRepository.deleteOlderThanSixHours();
		this.logUtility.logMessage("TTL Cleanup: deleted: " + deleted + " processed_webhooks older than 6 hours", INFO,
				log);

	}

}
