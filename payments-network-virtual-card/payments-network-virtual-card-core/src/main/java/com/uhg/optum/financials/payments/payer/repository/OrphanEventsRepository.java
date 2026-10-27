package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.OrphanEvents;
import org.springframework.data.jpa.repository.JpaRepository;

public interface OrphanEventsRepository extends JpaRepository<OrphanEvents, Long> {

}
