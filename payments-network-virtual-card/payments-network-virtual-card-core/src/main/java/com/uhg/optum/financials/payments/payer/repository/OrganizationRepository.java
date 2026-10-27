package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.events.Organization;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.UUID;

@Repository
public interface OrganizationRepository extends JpaRepository<Organization, UUID> {

	Organization findByOrgId(UUID orgId);

	@Query("SELECT o.orgShortCode FROM Organization o WHERE o.orgId = :orgId")
	String findOrgShortCodeByOrgId(@Param("orgId") UUID orgId);

	Organization findByOrgShortCode(String orgShortCode);

	@Transactional
	@Modifying
	@Query(value = "UPDATE virtual_card.organizations SET org_status = 'CLOSED', updated_on = CURRENT_TIMESTAMP WHERE org_id = :orgId",
			nativeQuery = true)
	int closeOrganizationById(@Param("updateById") String updateById, @Param("orgId") UUID orgId);

	@Query("SELECT " + "CASE WHEN COUNT(o) > 0 THEN true ELSE false END, "
			+ "CASE WHEN COUNT(a) > 0 THEN true ELSE false END, " + "CASE WHEN COUNT(s) > 0 THEN true ELSE false END "
			+ "FROM Organization o "
			+ "LEFT JOIN Account a ON o.orgShortCode = a.orgShortCode AND a.accountShortCode = :accountShortCode "
			+ "LEFT JOIN SubAccount s ON a.accountShortCode = s.accountShortCode AND s.subAccountShortCode = :subAccountShortCode "
			+ "WHERE o.orgShortCode = :orgShortCode")
	List<List<Boolean>> checkOrgAccountSubAccountExistence(@Param("orgShortCode") String orgShortCode,
			@Param("accountShortCode") String accountShortCode,
			@Param("subAccountShortCode") String subAccountShortCode);

	@Query("SELECT " + "CASE WHEN COUNT(o.orgId) > 0 THEN o.name ELSE NULL END, "
			+ "CASE WHEN COUNT(a.accountId) > 0 THEN a.name ELSE NULL END, "
			+ "CASE WHEN COUNT(s.subAccountId) > 0 THEN s.name ELSE NULL END " + "FROM Organization o "
			+ "LEFT JOIN Account a ON o.orgShortCode = a.orgShortCode AND a.accountShortCode = :accountShortCode "
			+ "LEFT JOIN SubAccount s ON a.accountShortCode = s.accountShortCode AND s.subAccountShortCode = :subAccountShortCode "
			+ "WHERE o.orgShortCode = :orgShortCode GROUP BY o.name, a.name, s.name")
	List<List<String>> getOrgAccountSubAccountNames(@Param("orgShortCode") String orgShortCode,
			@Param("accountShortCode") String accountShortCode,
			@Param("subAccountShortCode") String subAccountShortCode);

	boolean existsByOrgId(UUID orgId);

}
