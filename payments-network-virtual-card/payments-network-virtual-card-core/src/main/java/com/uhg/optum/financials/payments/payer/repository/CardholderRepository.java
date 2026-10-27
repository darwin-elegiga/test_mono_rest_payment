package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.CardHolder;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

@Repository
public interface CardholderRepository extends JpaRepository<CardHolder, Long> {

	@Query("SELECT c FROM CardHolder c")
	CardHolder findCardholder();

}
