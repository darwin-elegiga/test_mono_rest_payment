package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Getter
@Setter
@Table(name = "clear_extract", schema = "virtual_card")
public class ClearExtract {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	private Long id;

	@Column(name = "file_id", nullable = false, length = 100)
	private String fileId;

	@Column(name = "file_name", nullable = false, length = 200)
	private String fileName;

	@Column(name = "message_sequence_number", nullable = false, length = 200)
	private String messageSequenceNumber;

	@Column(name = "original_auth_request_id", length = 256)
	private String originalAuthId;

	@Column(name = "network", nullable = false, length = 20)
	private String network;

	@Column(name = "network_transaction_id", length = 100)
	private String networkTransactionId;

	@Column(name = "receipt_id", length = 20)
	private String receiptId;

	@Column(name = "acquirer_institution_id", length = 6)
	private String acquirerInstitutionId;

	@Column(name = "ifi_id", nullable = false, length = 10)
	private String ifiId;

	@Column(name = "ifi_name", nullable = false, length = 40)
	private String ifiName;

	@Column(name = "bin", length = 10)
	private String bin;

	@Column(name = "card_id")
	private UUID cardId;

	@Column(name = "masked_pan_number", length = 19)
	private String maskedPanNumber;

	@Column(name = "card_status", length = 10)
	private String cardStatus;

	@Column(name = "account_holder_id", length = 40)
	private String accountHolderId;

	@Column(name = "settlement_date", length = 6)
	private String settlementDate;

	@Column(name = "settlement_indicator", length = 8)
	private String settlementIndicator;

	@Column(name = "approval_code", length = 6)
	private String approvalCode;

	@Column(name = "retrieval_reference_number", length = 50)
	private String retrievalReferenceNumber;

	@Column(name = "acquirer_reference_number", nullable = false, length = 50)
	private String acquirerReferenceNumber;

	@Column(name = "clearing_status", nullable = false, length = 50)
	private String clearingStatus;

	@Column(name = "clearing_type", nullable = false, length = 50)
	private String clearingType;

	@Column(name = "clearing_sub_category", length = 64)
	private String clearingSubCategory;

	@Column(name = "purchase_date", length = 12)
	private String purchaseDate;

	@Column(name = "clearing_date", nullable = false)
	private LocalDateTime clearingDate;

	@Column(name = "clearing_amount", precision = 20, scale = 2)
	private BigDecimal clearingAmount;

	@Column(name = "clearing_currency", length = 3)
	private String clearingCurrency;

	@Column(name = "billing_amount", precision = 20, scale = 2)
	private BigDecimal billingAmount;

	@Column(name = "billing_currency", length = 3)
	private String billingCurrency;

	@Column(name = "failure_reason", length = 200)
	private String failureReason;

	@Column(name = "geo_indicator", length = 13)
	private String geoIndicator;

	@Column(name = "merchant_id", length = 200)
	private String merchantId;

	@Column(name = "merchant_name", length = 40)
	private String merchantName;

	@Column(name = "merchant_terminal_id", length = 8)
	private String merchantTerminalId;

	@Column(name = "merchant_category_code", length = 4)
	private String merchantCategoryCode;

	@Column(name = "merchant_city", length = 40)
	private String merchantCity;

	@Column(name = "merchant_country_code", length = 3)
	private String merchantCountryCode;

	@Column(name = "interchange_fee_amount", precision = 20, scale = 2)
	private BigDecimal interchangeFeeAmount;

	@Column(name = "interchange_fee_sign", length = 1)
	private String interchangeFeeSign;

	@Column(name = "interchange_fee_currency", length = 3)
	private String interchangeFeeCurrency;

	@Column(name = "payment_code", length = 20)
	private String paymentCode;

	@Column(name = "payment_code_name", length = 50)
	private String paymentCodeName;

	@Column(name = "channel_code", length = 20)
	private String channelCode;

	@Column(name = "channel_code_name", length = 50)
	private String channelCodeName;

	@Column(name = "mti", nullable = false, length = 4)
	private String mti;

	@Column(name = "processing_code", length = 10)
	private String processingCode;

	@Column(name = "function_code", length = 10)
	private String functionCode;

	@Column(name = "record_status", length = 50)
	private String recordStatus;

	@Column(name = "created_at", nullable = false)
	private LocalDateTime createdAt;

	@Column(name = "created_by", length = 40)
	private String createdBy;

	@Column(name = "updated_at")
	private LocalDateTime updatedAt;

	@Column(name = "updated_by", length = 40)
	private String updatedBy;

}
