package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.IdClass;
import jakarta.persistence.Table;
import jakarta.validation.constraints.Size;
import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.util.UUID;

@Entity
@Getter
@Setter
@IdClass(AuthExtractId.class)
@Table(name = "auth_extract", schema = "virtual_card")
public class AuthExtract {

	@Id
	@Column(name = "receipt_id", length = 20, nullable = false)
	private String receiptId;

	@Id
	@Column(name = "card_id", nullable = false)
	private UUID cardId;

	@Column(name = "original_auth_request_id", length = 256, nullable = false)
	private String originalAuthRequestId;

	@Column(name = "payment_request_id", length = 256)
	private String paymentRequestId;

	@Column(name = "network_transaction_id", length = 100)
	private String networkTransactionId;

	@Column(name = "retrieval_reference_number", length = 50)
	private String retrievalReferenceNumber;

	@Column(name = "stan", length = 6)
	private String stan;

	@Column(name = "approval_code", length = 6)
	private String approvalCode;

	@Column(name = "system_auth_time", nullable = false)
	private OffsetDateTime systemAuthTime;

	@Column(name = "merchant_auth_time", length = 20)
	private String merchantAuthTime;

	@Column(name = "auth_expiry_time", length = 20)
	private String authExpiryTime;

	@Column(name = "bin", length = 10)
	private String bin;

	@Column(name = "ifi_id", length = 10, nullable = false)
	private String ifiId;

	@Column(name = "ifi_name", length = 40)
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

	@Column(name = "geo_indicator", length = 13)
	private String geoIndicator;

	@Column(name = "dcc_indicator", length = 5)
	private String dccIndicator;

	@Column(name = "pos_entry_mode", length = 3)
	private String posEntryMode;

	@Column(name = "pos_transaction_code", length = 100)
	private String posTransactionCode;

	@Column(name = "pin_present_indicator", length = 1)
	private String pinPresentIndicator;

	@Column(name = "pin_valid_indicator", length = 1)
	private String pinValidIndicator;

	@Column(name = "cvv2_present_indicator", length = 1)
	private String cvv2PresentIndicator;

	@Column(name = "cvv2_valid_indicator", length = 1)
	private String cvv2ValidIndicator;

	@Column(name = "ecom_indicator", length = 1)
	private String ecomIndicator;

	@Column(name = "acs_correlation_id", length = 100)
	private String acsCorrelationId;

	@Column(name = "eci_indicator", length = 3)
	private String eciIndicator;

	@Column(name = "auth_action_code", length = 50)
	private String authActionCode;

	@Column(name = "response_code", length = 2)
	private String responseCode;

	@Column(name = "decline_reason", length = 256)
	private String declineReason;

	@Column(length = 256)
	@Size(max = 256)
	private String declineErrorCode;

	@Column(name = "moto_indicator", length = 1)
	private String motoIndicator;

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

	@Column(name = "merchant_name", length = 40)
	private String merchantName;

	@Column(name = "merchant_city", length = 40)
	private String merchantCity;

	@Column(name = "merchant_state", length = 30)
	private String merchantState;

	@Column(name = "merchant_country", length = 10)
	private String merchantCountry;

	@Column(name = "payment_code", length = 20)
	private String paymentCode;

	@Column(name = "payment_code_name", length = 50)
	private String paymentCodeName;

	@Column(name = "channel_code", length = 20)
	private String channelCode;

	@Column(name = "channel_code_name", length = 50)
	private String channelCodeName;

	@Column(length = 50)
	@Size(max = 50)
	private String transactionStatus;

	@Column(length = 50)
	@Size(max = 50)
	private String transactionType;

	@Column(name = "clearing_status", length = 50)
	private String clearingStatus;

	@Column(name = "created_at", nullable = false)
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 40)
	private String createdBy;

	@Column(name = "updated_at")
	private LocalDateTime updatedAt;

	@Column(name = "updated_by", length = 40)
	private String updatedBy;

}
