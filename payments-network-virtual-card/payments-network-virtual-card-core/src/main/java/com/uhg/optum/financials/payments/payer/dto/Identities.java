package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class Identities {

	private String email; // Mandatory

	private String phoneNumber; // Optional

}
