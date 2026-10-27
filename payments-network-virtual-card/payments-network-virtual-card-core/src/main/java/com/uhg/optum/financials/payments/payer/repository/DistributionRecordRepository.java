package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.DistributionRecord;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface DistributionRecordRepository extends JpaRepository<DistributionRecord, Long> {

	List<DistributionRecord> findByDistributionBatchId(Long distributionBatchId);

}
