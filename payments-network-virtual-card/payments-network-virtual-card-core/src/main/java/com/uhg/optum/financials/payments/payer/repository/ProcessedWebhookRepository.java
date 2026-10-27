package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.ProcessedWebhookDetails;
import com.uhg.optum.financials.payments.payer.model.ProcessedWebhookDetailsId;
import jakarta.transaction.Transactional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface ProcessedWebhookRepository extends JpaRepository<ProcessedWebhookDetails, ProcessedWebhookDetailsId> {

	@Modifying(clearAutomatically = true, flushAutomatically = true)
	@Transactional
	@Query(value = """
			INSERT INTO virtual_card.processed_webhooks (id, event_type, processed_at, processed_status)
			VALUES (:id, :eventType, now(), :processedStatus)
			ON CONFLICT (id, event_type, processed_status) DO NOTHING
			""", nativeQuery = true)
	int tryInsertProcessed(@Param("id") String id, @Param("eventType") String eventType,
			@Param("processedStatus") String processedStatus);

	@Modifying
	@Transactional
	@Query(value = """
			DELETE FROM virtual_card.processed_webhooks
			WHERE processed_at < now() - interval '6 hours'
			""", nativeQuery = true)
	int deleteOlderThanSixHours();

}
