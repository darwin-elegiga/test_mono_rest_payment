package com.uhg.optum.financials.payments.payer.dto.document;

import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.payments.payer.model.PayeeDetails;
import lombok.Getter;
import lombok.Setter;

import java.util.List;

@Getter
@Setter
public class TemplateMappedValues {

	@Getter
	@Setter
	private String paymentType;

	@Getter
	@Setter
	private String payerName;

	@Getter
	@Setter
	private String paymentIdentifier;

	@Getter
	@Setter
	private SubAccountDto subAccountDto;

	@Getter
	@Setter
	private PayeeDetails payeeDetails;

	@Getter
	@Setter
	private String issuerPaymentIdList;

	@Getter
	@Setter
	private String networkPaymentId;

	@Getter
	@Setter
	private String amount;

	@Getter
	@Setter
	private String date;

	@Getter
	@Setter
	private String logoUrl;

	@Getter
	@Setter
	private String optumLogo;

	@Getter
	@Setter
	private String cardTypeLogo;

	@Getter
	@Setter
	private String customRemarks1;

	@Getter
	@Setter
	private List<String> customRemarks2;

	@Getter
	@Setter
	private String expiryDate;

	@Getter
	@Setter
	private String pin;

	@Getter
	@Setter
	private String barCodeUrl;

	@Getter
	@Setter
	private String instructions;

	@Getter
	@Setter
	private String cardLastFourDigits;

	@Getter
	@Setter
	private String cvv;

	@Getter
	@Setter
	private String expiry;

}
