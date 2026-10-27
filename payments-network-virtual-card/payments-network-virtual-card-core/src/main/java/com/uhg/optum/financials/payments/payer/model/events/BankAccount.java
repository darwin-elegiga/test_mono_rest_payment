package com.uhg.optum.financials.payments.payer.model.events;

import com.uhg.optum.financials.payments.payer.model.BaseEntity;
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
@Table(name = "bank_accounts", schema = "virtual_card")
public class BankAccount extends BaseEntity {

	@Id
	private UUID bankAccountId;

	@Getter
	@Setter
	@Column(name = "account_type")
	private String accountType;

	@Getter
	@Setter
	@Column(name = "verification_status")
	private String verificationStatus;

	@Getter
	@Setter
	@Column(name = "account_number_last_four_digits")
	private String accountNumberLastFourDigits;

	@Getter
	@Setter
	@Column(name = "routing_number")
	private String routingNumber;

	@Getter
	@Setter
	@Column(name = "account_name")
	private String name;

	@Getter
	@Setter
	@Column(name = "display_name")
	private String displayName;

	@Getter
	@Setter
	private UUID orgId;

	@Getter
	@Setter
	private String type;

	@Column(name = "account_number")
	private String accountNumber;

	@Column(name = "key_name")
	private String keyName;

	@Column(name = "org_short_code")
	private String orgShortCode;

	@Column(name = "created_on")
	private LocalDateTime createdOn;

	@Column(name = "updated_on")
	private LocalDateTime updatedOn;

	@Column(name = "created_by")
	private String createdBy;

	@Column(name = "updated_by")
	private String updatedBy;

	@Column(name = "encryption_at_rest_version")
	private String encryptionAtRestVersion;

}
