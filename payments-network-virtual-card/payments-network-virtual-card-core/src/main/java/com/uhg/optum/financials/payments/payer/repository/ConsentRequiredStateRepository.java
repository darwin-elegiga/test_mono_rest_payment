package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.ConsentRequiredStates;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface ConsentRequiredStateRepository extends JpaRepository<ConsentRequiredStates, Long> {

	List<ConsentRequiredStates> findAllByOrderByStateNameAsc();

	Optional<ConsentRequiredStates> findByStateCode(String stateCode);

}
