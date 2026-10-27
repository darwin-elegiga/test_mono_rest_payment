package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.AuditLog;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.UUID;

@Repository
public interface AuditLogRepository extends JpaRepository<AuditLog, Long> {

	List<AuditLog> findByCardGuid(UUID cardGuid);

	List<AuditLog> findByRequestId(UUID requestId);

	List<AuditLog> findByEventType(String eventType);

	List<AuditLog> findByStatus(String status);

	List<AuditLog> findBySource(String source);

	List<AuditLog> findByPaymentId(String paymentId);

	List<AuditLog> findByPaymentIdOrderByCreatedAtAsc(String paymentId);

}
