package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.Bin;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface BinRepository extends JpaRepository<Bin, Long> {

	Optional<Bin> findByFundingType(String fundingType);

	boolean existsByFundingType(String fundingType);

	boolean existsByFundingTypeAndIdNot(String fundingType, Long id);

}
