package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.events.LocalBankAccount;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.UUID;

@Repository
public interface LocalBankAccountRepository extends JpaRepository<LocalBankAccount, UUID> {

	LocalBankAccount findByLocalBankAccountId(UUID localBankAccountId);

}
