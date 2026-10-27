package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;

@Getter
@Setter
public class ProviderStateConsentDto {

	private Long id;

	private String taxId;

	private String stateCode;

	private Boolean isActive;

	private LocalDateTime createdAt;

	private String createdBy;

	private LocalDateTime updatedAt;

	private String updatedBy;

}
