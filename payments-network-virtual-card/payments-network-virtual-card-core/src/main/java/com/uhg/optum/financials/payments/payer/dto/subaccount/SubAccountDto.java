package com.uhg.optum.financials.payments.payer.dto.subaccount;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
@JsonIgnoreProperties(ignoreUnknown = true)
public class SubAccountDto {

	SubAccountRegistered subAccountRegistered;

}
