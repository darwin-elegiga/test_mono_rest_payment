package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.ProviderStateConsent;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface ProviderStateConsentRepository extends JpaRepository<ProviderStateConsent, Long> {

	Optional<ProviderStateConsent> findByTaxIdAndStateCodeAndIsActiveTrue(String taxId, String stateCode);

	Optional<ProviderStateConsent> findByTaxIdAndStateCode(String taxId, String stateCode);

}
