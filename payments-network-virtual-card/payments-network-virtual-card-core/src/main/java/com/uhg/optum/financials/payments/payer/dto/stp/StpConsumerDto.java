package com.uhg.optum.financials.payments.payer.dto.stp;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
@JsonIgnoreProperties(ignoreUnknown = true)
@JsonInclude(JsonInclude.Include.NON_NULL)
public class StpConsumerDto implements Serializable {

	private String patientName;

	private String patientDOB;

	private String claimId;

	private String patientAccountNumber;

	private String amount;

	private String claimDate;

	private String subscriberName;

	private String clientReferenceId;

}
