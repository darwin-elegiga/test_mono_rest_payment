package com.uhg.optum.financials.payments.payer.dto.authevent;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class MerchantInfo {

	private String merchantId;

	private String merchantTerminalId;

	private String merchantCategoryCode;

	private String merchantName;

	private String merchantCity;

	private String merchantState;

	private String merchantCountry;

}
