package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.uhg.optum.financials.shared.client.constant.PaymentType;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.List;

import static com.uhg.optum.financials.payments.payer.util.Utility.getBigDecimal;
import static com.uhg.optum.financials.payments.payer.util.Utility.getString;

@Getter
@Setter
@NoArgsConstructor
public class CompanionDto {

	private String networkPaymentID;

	private String summaryPageURL;

	private String paymentType;

	@JsonProperty("sub_account_short_cd")
	private String subAccountShortCode;

	@JsonProperty("account_short_cd")
	private String accountShortCode;

	private List<IssuerPayment> issuerPayments;

	private String summaryPageUnmaskedURL;

	@JsonIgnore
	private BigDecimal amount;

	public CompanionDto(Object[] rs) {
		this.networkPaymentID = rs[0].toString();
		this.amount = getBigDecimal(rs[4], null);
		this.setPaymentTypeByType(getString(rs[1], null));
		this.setIssuerPayments(getString(rs[2], null), getString(rs[3], null));
		if (rs.length > 5) {
			this.subAccountShortCode = getString(rs[5], null);
		}
		if (rs.length > 6) {
			this.accountShortCode = getString(rs[6], null);
		}

	}

	public void setPaymentTypeByType(String type) {
		if (type == null || type.isEmpty()) {
			return;
		}
		switch (PaymentType.fromString(type.trim())) {
			case PAYMENT -> this.paymentType = "I";
			case PAYMENT_ONLY -> this.paymentType = (isAmountPositive()) ? "D" : "H";
			case NOTIFICATION -> this.paymentType = "H";
			case REMITTANCE_ONLY -> this.paymentType = (isAmountPositive()) ? "X" : "H";
			case C2B -> this.paymentType = "C2B";
		}
	}

	private boolean isAmountPositive() {
		return this.amount != null && this.amount.signum() > 0;
	}

	public void setIssuerPayments(String issuerPaymentIds, String paymentIds) {
		if (issuerPaymentIds == null || issuerPaymentIds.isEmpty() || paymentIds == null || paymentIds.isEmpty()) {
			return;
		}

		String[] issuerIds = issuerPaymentIds.split(",");
		String[] payments = paymentIds.split(",");

		// Ensure we only process the number of elements available in both arrays
		int minLength = Math.min(issuerIds.length, payments.length);

		this.issuerPayments = new ArrayList<>();
		for (int i = 0; i < minLength; i++) {
			CompanionDto.IssuerPayment issuerPayment = new CompanionDto.IssuerPayment();
			issuerPayment.setIssuerPaymentID(issuerIds[i].trim());
			issuerPayment.setPaymentId(payments[i].trim());
			this.issuerPayments.add(issuerPayment);
		}
	}

	@Getter
	@Setter
	public static final class IssuerPayment {

		private String issuerPaymentID;

		private String paymentId;

	}

}
