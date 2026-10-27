package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
public class AvailableBalance implements Serializable {

	private static final long serialVersionUID = 1L;

	private Float amount;

	private String currency;

}
