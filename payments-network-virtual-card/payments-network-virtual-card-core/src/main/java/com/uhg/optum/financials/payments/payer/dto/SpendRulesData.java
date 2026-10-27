package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

import java.util.List;

@Setter
@Getter
public class SpendRulesData {

	private String orgShortCd;

	private String orgName;

	private String accShortCd;

	private String acctName;

	private String subAccShortCd;

	private String subAcctName;

	private List<String> spendRules;

	private String createdBy;

	private String lastUpdatedBy;

	private String createdDate;

	private String lastUpdatedDate;

}
