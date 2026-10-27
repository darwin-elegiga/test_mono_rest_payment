package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.VirtualCardHistory;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.UUID;

@Repository
public interface VirtualCardHistoryRepository extends JpaRepository<VirtualCardHistory, Long> {

	List<VirtualCardHistory> findByCardGuidOrderByCreatedAtAsc(UUID cguid);

}
