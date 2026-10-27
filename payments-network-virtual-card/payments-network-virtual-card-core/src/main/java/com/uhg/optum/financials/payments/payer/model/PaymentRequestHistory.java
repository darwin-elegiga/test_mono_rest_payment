package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.shared.client.dto.payment.DocumentDto;
import com.uhg.optum.financials.shared.client.mapper.DocumentDtoMapper;
import com.uhg.optum.financials.shared.client.mapper.MetadataMapper;
import jakarta.persistence.Column;
import jakarta.persistence.Convert;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;
import java.util.Map;

@Entity
@Getter
@Setter
@Table(name = "payment_request_history", schema = "virtual_card")
public class PaymentRequestHistory {

	@Id
	@GeneratedValue(strategy = GenerationType.IDENTITY)
	@Column(name = "id")
	private Long id;

	@Column(name = "payr835_id", length = 36)
	private String payr835id;

	@Column(columnDefinition = "TEXT")
	private String stSeSegment;

	@Column(length = 4, nullable = false)
	private String status;

	@Column(length = 50)
	private String submitterId;

	@Column(name = "network_payment_id", length = 50)
	private String networkPaymentId;

	@Column(name = "issuer_payment_id")
	private String issuerPaymentId;

	@Column(name = "amount", columnDefinition = "DECIMAL(19,4)")
	private BigDecimal amount;

	@Column(name = "reference_id")
	private String referenceId;

	@Column(name = "remit_method", length = 4)
	private String remitMethod;

	@Column(name = "payment_details_id", length = 32)
	private String paymentDetailsId;

	@Column(length = 36, nullable = false)
	private String origin;

	@Column(name = "org_short_code", nullable = false)
	private String orgShortCode;

	@Column(name = "account_short_code", nullable = false)
	private String accountShortCode;

	@Column(name = "sub_account_short_code", nullable = false)
	private String subAccountShortCode;

	@Column(name = "correlation_id", length = 32, nullable = true)
	private String correlationId;

	@Getter
	@Setter
	private int totalClmCount;

	@Getter
	@Setter
	@Column(name = "clm_url", length = 500)
	private String clmUrl;

	@Getter
	@Setter
	private String docType;

	@Getter
	@Setter
	private boolean plb;

	@Convert(converter = DocumentDtoMapper.class)
	@Column(length = 500)
	private List<DocumentDto> documents;

	@Convert(converter = MetadataMapper.class)
	@Column(length = 500)
	private Map<String, String> metadata;

	@Column(length = 40)
	private String createdById;

	@Column(name = "re_preference_flag", nullable = false)
	private boolean rePreferenceFlag;

	@Column(name = "error_message", columnDefinition = "TEXT")
	private String errorMessage;

	@Getter
	@Setter
	private LocalDateTime createdOn;

}
