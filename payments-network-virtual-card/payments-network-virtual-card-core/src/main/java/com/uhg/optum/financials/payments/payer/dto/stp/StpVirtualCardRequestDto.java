package com.uhg.optum.financials.payments.payer.dto.stp;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonInclude;
import jakarta.validation.Valid;
import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
@JsonIgnoreProperties(ignoreUnknown = true)
@JsonInclude(JsonInclude.Include.NON_NULL)
public class StpVirtualCardRequestDto implements Serializable {

	@Valid
	private StpRequestHeaderDto header;

	@Valid
	private StpConsumerDto consumer;

	@Valid
	private StpPayeeDto payee;

	@Valid
	private StpPayerDto payer;

}
