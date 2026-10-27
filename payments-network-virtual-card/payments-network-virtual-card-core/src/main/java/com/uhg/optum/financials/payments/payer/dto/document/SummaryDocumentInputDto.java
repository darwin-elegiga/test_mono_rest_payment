package com.uhg.optum.financials.payments.payer.dto.document;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDate;

@Getter
@Setter
@AllArgsConstructor
public class SummaryDocumentInputDto {

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
	private String payeeName;

	@Getter
	@Setter
	private String payeeTaxId;

	@Getter
	@Setter
	private String payeeNpi;

	@Getter
	@Setter
	private String payeeReceiverId;

	@Getter
	@Setter
	private String payeeType;

	@Getter
	@Setter
	private String addressLine1;

	@Getter
	@Setter
	private String addressLine2;

	@Getter
	@Setter
	private String addressCity;

	@Getter
	@Setter
	private String addressState;

	@Getter
	@Setter
	private String addressCountry;

	@Getter
	@Setter
	private String addressPostalCode;

	@Getter
	@Setter
	private LocalDate paymentDate;

	@Getter
	@Setter
	private String networkPaymentId;

}
