package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.CardReset;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.UUID;

@Repository
public interface CardResetRepository extends JpaRepository<CardReset, UUID> {

	CardReset findByReceiptIdAndCardGuid(String receiptId, UUID cardGuid);

	CardReset findTopByReceiptIdAndCardGuidOrderByCreatedAtDesc(String receiptId, UUID cardGuid);

	CardReset findByReceiptId(String receiptId);

}
