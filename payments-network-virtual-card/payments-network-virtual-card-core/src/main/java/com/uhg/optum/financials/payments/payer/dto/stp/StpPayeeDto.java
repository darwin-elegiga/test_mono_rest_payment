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
public class StpPayeeDto implements Serializable {

	private String name;

	private String tin;

	private String uuid;

	private String addressLine1;

	private String city;

	private String state;

	private String zip;

}
