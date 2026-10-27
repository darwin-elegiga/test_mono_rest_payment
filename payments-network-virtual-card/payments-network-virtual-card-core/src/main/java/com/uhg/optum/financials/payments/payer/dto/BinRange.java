package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class BinRange {

	private String bin;

	private String network;

	private String rangeID;

	private int binLength;

	private String binType;

}
