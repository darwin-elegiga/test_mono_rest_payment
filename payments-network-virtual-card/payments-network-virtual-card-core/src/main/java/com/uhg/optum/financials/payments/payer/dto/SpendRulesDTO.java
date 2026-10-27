package com.uhg.optum.financials.payments.payer.dto;

import jakarta.validation.constraints.NotNull;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
public class SpendRulesDTO {

	private List<SpendRulesAccountDto> accounts;

	@NotNull
	private List<String> spendRules;

	private String createdBy;

	private String updatedBy;

}
