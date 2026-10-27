package com.uhg.optum.financials.payments.payer.dto.stp;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
@JsonIgnoreProperties(ignoreUnknown = true)
public class StpEnrollmentRecordDto implements Serializable {

	private String npi;

	private String uuid;

	private String address1;

	private String address2;

	private String city;

	private String state;

	private String zip9;

	private String enrollmentType;

	private String enrollmentEligibilityStatus;

	private String effectiveDate;

	private String initialEnrollmentDate;

	private String lastUpdatedUTCDate;

	private String enrollmentStatusCd;

	private String enrollmentStatusReason;

}
