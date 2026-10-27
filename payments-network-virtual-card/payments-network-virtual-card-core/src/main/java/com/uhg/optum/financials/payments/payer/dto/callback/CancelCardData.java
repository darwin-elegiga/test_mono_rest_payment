package com.uhg.optum.financials.payments.payer.dto.callback;

import lombok.Getter;
import lombok.Setter;

import java.util.UUID;

@Getter
@Setter
public class CancelCardData {

	private UUID clientRequestId;

	private UUID cardGuid;

	private String reasonCode;

}
