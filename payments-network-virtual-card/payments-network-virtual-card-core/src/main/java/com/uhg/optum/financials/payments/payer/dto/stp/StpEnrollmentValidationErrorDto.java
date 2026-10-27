package com.uhg.optum.financials.payments.payer.dto.stp;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
public class StpEnrollmentValidationErrorDto implements Serializable {

	private String providerTin;

	private String npi;

	private String enrollmentType;

	private String field;

	private String code;

	private String message;

}
