package com.uhg.optum.financials.payments.payer.dto;

import lombok.Data;

@Data
public class BinResponse {

	private Long id;

	private Long bin;

	private String fundingType;

}
