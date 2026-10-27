package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.payments.payer.validation.AccShortCdValidator;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
public class SpendRulesAccountDto {

	@AccShortCdValidator
	private String accShortCd;

	private List<String> subAccounts;

}
