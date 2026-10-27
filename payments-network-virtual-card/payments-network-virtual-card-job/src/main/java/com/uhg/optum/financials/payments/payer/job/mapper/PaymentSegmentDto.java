package com.uhg.optum.financials.payments.payer.job.mapper;

import com.uhg.optum.financials.shared.client.projection.PaymentSegment;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class PaymentSegmentDto implements PaymentSegment {

	@Getter
	@Setter
	private String orgShortCode;

	@Getter
	@Setter
	private String accountShortCode;

	@Getter
	@Setter
	private String subAccountShortCode;

	@Getter
	@Setter
	private String payer835Id;

	@Getter
	@Setter
	private String submitterId;

	@Getter
	@Setter
	private String stSeSegment;

	@Getter
	@Setter
	private Double paymentAmount;

	@Getter
	@Setter
	private String networkPaymentId;

	@Getter
	@Setter
	private String payerName;

	@Getter
	@Setter
	private String payerShortCode;

	@Getter
	@Setter
	private String fileIdentifier;

	@Getter
	@Setter
	private String paymentId;

}
