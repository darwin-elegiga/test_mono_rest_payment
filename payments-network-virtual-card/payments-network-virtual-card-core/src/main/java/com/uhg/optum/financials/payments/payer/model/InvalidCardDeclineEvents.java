package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.Table;
import jakarta.persistence.Id;
import lombok.Getter;
import lombok.Setter;
import jakarta.persistence.GenerationType;

import java.math.BigDecimal;
import java.time.LocalDateTime;

@Entity
@Getter
@Setter
@Table(name = "invalid_card_decline_events", schema = "virtual_card")
public class InvalidCardDeclineEvents {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "network_transaction_id", length = 100)
	private String networkTransactionId;

	@Column(name = "retrieval_reference_number", length = 50)
	private String retrievalReferenceNumber;

	@Column(name = "stan", length = 6)
	private String stan;

	@Column(name = "system_auth_time", nullable = false)
	private LocalDateTime systemAuthTime;

	@Column(name = "time_local_transaction")
	private Integer timeLocalTransaction;

	@Column(name = "date_local_transaction")
	private Integer dateLocalTransaction;

	@Column(name = "bin", length = 10, nullable = false)
	private String bin;

	@Column(name = "ifi_id", length = 10, nullable = false)
	private String ifiId;

	@Column(name = "ifi_name", length = 40, nullable = false)
	private String ifiName;

	@Column(name = "card_sequence_number", length = 3)
	private String cardSequenceNumber;

	@Column(name = "payment_network", length = 20, nullable = false)
	private String paymentNetwork;

	@Column(name = "masked_card_number", length = 19, nullable = false)
	private String maskedCardNumber;

	@Column(name = "mti", length = 4, nullable = false)
	private String mti;

	@Column(name = "auth_amount", precision = 20, scale = 2)
	private BigDecimal authAmount;

	@Column(name = "auth_currency", length = 3)
	private String authCurrency;

	@Column(name = "billing_amount", precision = 20, scale = 2)
	private BigDecimal billingAmount;

	@Column(name = "billing_currency", length = 3)
	private String billingCurrency;

	@Column(name = "additional_data_private_use", length = 1000)
	private String additionalDataPrivateUse;

	@Column(name = "pos_entry_mode", length = 3)
	private String posEntryMode;

	@Column(name = "pos_data", length = 30)
	private String posData;

	@Column(name = "auth_action_code", length = 50)
	private String authActionCode;

	@Column(name = "response_code", length = 2)
	private String responseCode;

	@Column(name = "acquiring_institution_id", length = 6)
	private String acquiringInstitutionId;

	@Column(name = "forwarding_institution_id", length = 6)
	private String forwardingInstitutionId;

	@Column(name = "merchant_id", length = 200)
	private String merchantId;

	@Column(name = "merchant_terminal_id", length = 8)
	private String merchantTerminalId;

	@Column(name = "merchant_category_code", length = 4)
	private String merchantCategoryCode;

	@Column(name = "merchant_name_location", length = 100)
	private String merchantNameLocation;

	@Column(name = "created_at")
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 100)
	private String createdBy;

}
