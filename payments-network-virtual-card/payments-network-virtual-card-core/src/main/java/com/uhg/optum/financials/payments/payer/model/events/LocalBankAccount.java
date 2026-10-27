package com.uhg.optum.financials.payments.payer.model.events;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;
import java.util.UUID;

@Getter
@Setter
@Entity
@Table(name = "local_bank_accounts", schema = "virtual_card")
public class LocalBankAccount {

	@Id
	@Column(name = "local_bank_account_id")
	private UUID localBankAccountId;

	@Column(name = "account_name")
	private String accountName;

	@Column(name = "routing_number")
	private String routingNumber;

	@Column(name = "account_number")
	private String accountNumber;

	@Column(name = "account_number_last_four_digits")
	private String accountNumberLastFourDigits;

	@Column(name = "account_type")
	private String accountType;

	@Column(name = "verification_status")
	private String verificationStatus;

	@Column(name = "ach_company_id")
	private String achCompanyId;

	@Column(name = "created_on")
	private LocalDateTime createdOn;

	@Column(name = "updated_on")
	private LocalDateTime updatedOn;

	@Column(name = "type")
	private String type;

	@Column(name = "encryption_at_rest_version")
	private String encryptionAtRestVersion;

}
