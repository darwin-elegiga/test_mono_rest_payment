package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.DistributionBatch;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface DistributionBatchRepository extends JpaRepository<DistributionBatch, Long> {

	Optional<DistributionBatch> findByCorrelationId(String correlationId);

}
