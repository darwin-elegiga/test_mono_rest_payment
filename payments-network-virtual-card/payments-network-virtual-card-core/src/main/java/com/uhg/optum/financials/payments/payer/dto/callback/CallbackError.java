package com.uhg.optum.financials.payments.payer.dto.callback;

import lombok.Getter;
import lombok.Setter;

import java.io.Serializable;
import java.util.UUID;

@Getter
@Setter
public class CallbackError implements Serializable {

	private UUID clientRequestId;

	private String code;

	private String message;

	private String receiptId;

}
