package com.uhg.optum.financials.payments.payer.dto.stp;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.List;

@Getter
@Setter
public class StpEnrollmentCallbackEnrollmentErrorDto implements Serializable {

	private String providerTin;

	private String npi;

	private String enrollmentType;

	private List<String> errors;

}
