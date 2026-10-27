package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class SensitiveValue {

	private String serverPublicKey;

	private String encryptedData;

	private String iv;

}
