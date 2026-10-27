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
public class VirtualCardHistoryDto {

	private Long id;

	private UUID cardGuid;

	private String paymentId;

	private String status;

	private String receiptId;

	private String reason;

	private String currency;

	private BigDecimal initialBalance;

	private BigDecimal availableBalance;

	private BigDecimal currentBalance;

	private LocalDateTime createdAt;

	private String createdBy;

}
