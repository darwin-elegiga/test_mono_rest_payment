package com.uhg.optum.financials.payments.payer.dto.stp;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
public class StpProviderEnrollmentResponseHeaderDto implements Serializable {

	private String callbackUrl;

	private String correlationId;

	private String networkPartner;

	private String status;

}
