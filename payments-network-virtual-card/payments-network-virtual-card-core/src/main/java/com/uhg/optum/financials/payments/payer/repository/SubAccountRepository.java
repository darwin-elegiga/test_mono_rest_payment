package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountBankAccountDetails;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

import java.util.Optional;
import java.util.UUID;

@Repository
public interface SubAccountRepository extends JpaRepository<SubAccount, UUID> {

	SubAccount findByTin(String tin);

	SubAccount findBySubAccountId(UUID subAccountId);

	Optional<SubAccount> findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(String accountShortCd,
			String shortCode, String orgShortCode);

	@Transactional
	@Modifying
	@Query(value = "UPDATE virtual_card.sub_accounts  SET program_id = :programId ,updated_on = CURRENT_TIMESTAMP WHERE org_short_code = :orgShortCode",
			nativeQuery = true)
	void updateProgramId(@Param("programId") String programId, @Param("orgShortCode") String orgShortCode);

	Optional<SubAccount> findBySubAccountShortCode(String fundingMethod);

	@Query("SELECT new com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountBankAccountDetails(sa, bad.accountNumber, bad.routingNumber, a.payer835Id) FROM SubAccount sa LEFT JOIN BankAccount bad ON "
			+ "(case when sa.fundingMethod = 'NON_FUNDED' then sa.payFromBankAccount  else sa.fundingForeignAccount end) = bad.bankAccountId "
			+ "AND bad.orgShortCode = sa.orgShortCode LEFT JOIN Account a ON a.accountShortCode = sa.accountShortCode AND a.orgShortCode = sa.orgShortCode WHERE sa.orgShortCode = :orgShortCode AND sa.accountShortCode = :accountShortCode AND sa.subAccountShortCode = :subAccountShortCode")
	SubAccountBankAccountDetails findSubAccountBankDetails(@Param("orgShortCode") String orgShortCode,
			@Param("accountShortCode") String accountShortCode,
			@Param("subAccountShortCode") String subAccountShortCode);

	boolean existsByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(String accountShortCode,
			String subAccountShortcode, String orgShortCode);

	SubAccount findByOrgShortCodeAndSubAccountShortCodeAndAccountShortCode(String orgShortCode,
			String subAccountShortCode, String accountShortCd);

}
