package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.InvalidCardDeclineEvents;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface InvalidCardDeclineEventsRepository extends JpaRepository<InvalidCardDeclineEvents, Long> {

}
