package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.CompanyLogoDetails;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.util.UUID;

public interface CompanyLogoDetailsRepository extends JpaRepository<CompanyLogoDetails, UUID> {

	@Query("SELECT cld.logoUrl FROM CompanyLogoDetails cld WHERE cld.type= :type")
	String getLogoUrlByType(String type);

	CompanyLogoDetails findByType(String type);

}
