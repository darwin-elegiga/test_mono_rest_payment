package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.Getter;
import lombok.Setter;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.UUID;

@Getter
@Setter
@JsonInclude(JsonInclude.Include.NON_NULL)
public class AuditLogDto {

	private Long id;

	private String paymentId;

	private UUID cardGuid;

	private UUID requestId;

	private String receiptId;

	private String eventType;

	private String status;

	private String source;

	private BigDecimal amount;

	private LocalDateTime createdAt;

	private String createdBy;

}
