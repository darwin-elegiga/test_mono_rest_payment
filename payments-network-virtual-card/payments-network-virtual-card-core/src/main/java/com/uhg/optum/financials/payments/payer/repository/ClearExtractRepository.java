package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.ClearExtract;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface ClearExtractRepository extends JpaRepository<ClearExtract, Long> {

	ClearExtract findByFileId(String fileId);

	ClearExtract findByReceiptId(String receiptId);

	List<ClearExtract> findByReceiptIdIn(List<String> receiptIds);

}
