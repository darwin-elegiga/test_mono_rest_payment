package com.uhg.optum.financials.payments.payer.dto;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class ErrorResponse {

	private String traceId;

	private String errorCode;

	private String errorDescription;

	private String errorMessage;

	private Object additionalInfo;

}
