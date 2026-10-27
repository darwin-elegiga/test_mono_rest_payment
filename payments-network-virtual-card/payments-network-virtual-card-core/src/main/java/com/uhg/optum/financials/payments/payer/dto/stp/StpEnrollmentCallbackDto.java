package com.uhg.optum.financials.payments.payer.dto.stp;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.List;

@Getter
@Setter
public class StpEnrollmentCallbackDto implements Serializable {

	private StpEnrollmentCallbackHeaderDto header;

	private List<StpEnrollmentCallbackEnrollmentErrorDto> enrollmentErrors;

}
