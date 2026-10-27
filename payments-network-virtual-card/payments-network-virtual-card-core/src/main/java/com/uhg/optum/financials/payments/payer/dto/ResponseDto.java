package com.uhg.optum.financials.payments.payer.dto;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class ResponseDto<T extends Serializable> implements Serializable {

	@Getter
	@Setter
	private String message;

	@Getter
	@Setter
	private boolean status;

	@Getter
	@Setter
	private T payload;

}
