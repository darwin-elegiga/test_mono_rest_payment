package com.uhg.optum.financials.payments.payer.dto.stp;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;

@Getter
@Setter
public class StpProviderEnrollmentResponseDto implements Serializable {

	private StpProviderEnrollmentResponseHeaderDto header;

}
