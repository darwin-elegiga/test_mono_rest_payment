package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

public interface VirtualCardsRepository extends JpaRepository<VirtualCard, Long> {

	Optional<VirtualCard> findByCardGuid(UUID findByCardGuid);

	@Query("SELECT v.cardGuid FROM VirtualCard v WHERE v.paymentId = :paymentId")
	UUID findCardGuidByPaymentId(String paymentId);

	VirtualCard findByPaymentId(String paymentId);

	@Query("SELECT v.paymentId FROM VirtualCard v WHERE v.cardGuid = :cardGuid")
	String findPaymentIdByCardGuid(UUID cardGuid);

	boolean existsByCardGuid(UUID cardGuid);

	List<VirtualCard> findByCreatedAtBetween(LocalDateTime startDate, LocalDateTime endDate);

	List<VirtualCard> findByCreatedAtBetweenAndCardGuidIn(LocalDateTime startDate, LocalDateTime endDate,
			List<UUID> cardGuids);

}
