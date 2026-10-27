package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.LogoDetails;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.UUID;

@Repository
public interface LogoDetailsRepository extends JpaRepository<LogoDetails, UUID> {

	LogoDetails findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(String accountShortCode,
			String subAccountShortCode, String orgShortCode);

	@Query("SELECT ld.logoUrl FROM LogoDetails ld WHERE ld.orgShortCode = :orgShortCode AND ld.accountShortCode = :accountShortCode AND ld.subAccountShortCode IS NULL")
	String findLogoUrlByAccountShortCodeAndSubAccountShortCodeIsNullAndOrgShortCode(
			@Param("orgShortCode") String orgShortCode, @Param("accountShortCode") String accountShortCode);

	@Query("SELECT ld.logoUrl FROM LogoDetails ld WHERE ld.orgShortCode = :orgShortCode AND ld.accountShortCode IS NULL AND ld.subAccountShortCode IS NULL")
	String findLogoUrlByAccountShortCodeIsNullAndSubAccountShortCodeIsNullAndOrgShortCode(
			@Param("orgShortCode") String orgShortCode);

	@Query("SELECT ld.logoUrl FROM LogoDetails ld WHERE ld.orgShortCode = :orgShortCode AND ld.accountShortCode = :accountShortCode AND ld.subAccountShortCode = :subAccountShortCode")
	String findLogoUrlByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(
			@Param("orgShortCode") String orgShortCode, @Param("accountShortCode") String accountShortCode,
			@Param("subAccountShortCode") String subAccountShortCode);

}
