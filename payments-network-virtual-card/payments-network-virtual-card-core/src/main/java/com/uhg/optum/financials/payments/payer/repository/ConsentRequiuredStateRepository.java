package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.ConsentRequiredState;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface ConsentRequiuredStateRepository extends JpaRepository<ConsentRequiredState, String> {

	Optional<ConsentRequiredState> findConsentRequiredByStateCode(String stateCode);

	boolean existsByStateCodeAndConsentRequiredTrue(String stateCode);

}
