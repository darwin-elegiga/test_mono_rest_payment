package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.events.BankAccount;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.util.UUID;

public interface BankAccountRepository extends JpaRepository<BankAccount, UUID> {

	@Modifying
	@Transactional
	@Query("UPDATE BankAccount b SET b.verificationStatus = :verificationStatus, b.updatedOn = :updatedOn WHERE b.bankAccountId = :bankAccountId")
	int updateVerificationStatusAndUpdatedOn(UUID bankAccountId, int verificationStatus, LocalDateTime updatedOn);

	@Modifying
	@Transactional
	@Query("UPDATE BankAccount b SET " + "b.accountNumber = COALESCE(:accountNumber, b.accountNumber), "
			+ "b.routingNumber = COALESCE(:routingNumber, b.routingNumber), "
			+ "b.accountType = COALESCE(:accountType, b.accountType) " + "WHERE b.bankAccountId = :bankAccountId")
	int updateAccountDetails(UUID bankAccountId, String accountNumber, String routingNumber, Integer accountType);

	BankAccount findByBankAccountId(UUID bankAccountId);

	@Query("SELECT COUNT(ba) > 0 FROM BankAccount ba WHERE ba.bankAccountId = :bankAccountId")
	boolean existsByBankAccountId(UUID bankAccountId);

}
