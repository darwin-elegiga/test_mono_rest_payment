package com.uhg.optum.financials.payments.payer.dto.document;

import com.uhg.optum.financials.shared.client.events.IEvent;
import lombok.Data;

@Data
public class DocumentRequestCDE implements IEvent {

	private DocumentRequest documentRequest;

	private String cguid;

	private String paymentId;

	private String networkPaymentId;

	private String orgShortCode;

	private String bin;

	private String maskedCardNumber;

	private String type = "unmasked_summary_page";

	public String type() {
		return this.type;
	}

	private String issuerPaymentId;

}
