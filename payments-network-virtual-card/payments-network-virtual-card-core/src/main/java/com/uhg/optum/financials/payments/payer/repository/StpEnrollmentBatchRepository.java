package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.StpEnrollmentBatch;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface StpEnrollmentBatchRepository extends JpaRepository<StpEnrollmentBatch, Long> {

	boolean existsByCorrelationId(String correlationId);

	Optional<StpEnrollmentBatch> findByCorrelationId(String correlationId);

}
