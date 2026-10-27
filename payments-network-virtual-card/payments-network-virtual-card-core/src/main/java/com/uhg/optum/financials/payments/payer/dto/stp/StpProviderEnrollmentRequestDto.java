package com.uhg.optum.financials.payments.payer.dto.stp;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.List;

@Getter
@Setter
@JsonIgnoreProperties(ignoreUnknown = true)
public class StpProviderEnrollmentRequestDto implements Serializable {

	private StpEnrollmentHeaderDto header;

	private List<StpPartnerProviderDto> partnerProviders;

}
