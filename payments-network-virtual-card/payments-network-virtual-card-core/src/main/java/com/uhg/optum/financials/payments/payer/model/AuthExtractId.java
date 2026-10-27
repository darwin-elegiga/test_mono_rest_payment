package com.uhg.optum.financials.payments.payer.model;

import java.io.Serializable;
import java.util.UUID;
import lombok.EqualsAndHashCode;

@EqualsAndHashCode
public class AuthExtractId implements Serializable {

	private String receiptId;

	private UUID cardId;

}
