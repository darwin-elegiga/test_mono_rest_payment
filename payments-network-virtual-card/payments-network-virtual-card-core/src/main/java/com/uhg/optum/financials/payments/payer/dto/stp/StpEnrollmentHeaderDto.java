package com.uhg.optum.financials.payments.payer.dto.stp;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
@JsonIgnoreProperties(ignoreUnknown = true)
public class StpEnrollmentHeaderDto implements Serializable {

	private String networkPartner;

	private String correlationId;

	private Integer count;

}
