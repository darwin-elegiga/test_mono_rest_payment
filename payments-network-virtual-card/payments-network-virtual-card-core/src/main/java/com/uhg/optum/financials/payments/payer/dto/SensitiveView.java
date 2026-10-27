package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class SensitiveView {

	private String algo;

	private SensitiveValue value;

}
