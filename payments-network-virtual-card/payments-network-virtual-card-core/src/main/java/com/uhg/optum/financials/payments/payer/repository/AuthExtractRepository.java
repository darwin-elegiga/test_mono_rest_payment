package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.AuthExtractId;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

@Repository
public interface AuthExtractRepository extends JpaRepository<AuthExtract, AuthExtractId> {

	AuthExtract findByReceiptIdAndCardId(String receiptId, UUID cardId);

	AuthExtract findByReceiptId(String receiptId);

	List<AuthExtract> findByReceiptIdIn(List<String> receiptIds);

	Optional<AuthExtract> findByOriginalAuthRequestId(String originalAuthId);

	@Query("SELECT a.cardId FROM AuthExtract a WHERE a.receiptId = :receiptId")
	UUID findCardIdByReceiptId(String receiptId);

	List<AuthExtract> findByCardIdInAndCreatedAtBetweenOrderByCreatedAtAsc(List<UUID> cardIds, LocalDateTime startDate,
			LocalDateTime endDate);

	List<AuthExtract> findByCreatedAtBetweenOrderByCreatedAtAsc(LocalDateTime startDate, LocalDateTime endDate);

}
