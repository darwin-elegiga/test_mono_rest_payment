package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

public class SetECCKeysDto implements Serializable {

	@Setter
	@Getter
	private boolean setECCKeys;

	@Getter
	@Setter
	private boolean rotateECCKeys;

}
