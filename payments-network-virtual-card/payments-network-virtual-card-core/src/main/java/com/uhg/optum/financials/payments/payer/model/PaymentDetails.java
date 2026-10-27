package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import com.uhg.optum.financials.shared.client.model.BasePaymentDetails;
import com.uhg.optum.financials.shared.client.util.DateTimeUtil;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

@Entity
@Getter
@Setter
@Table(name = "payment_details", schema = "virtual_card")
public class PaymentDetails extends BasePaymentDetails {

	@Column(name = "network_payment_id", length = 50)
	private String networkPaymentId;

	@Column(name = "pin", length = 50)
	private String pin;

	public PaymentDetailsDto toDto() {
		PaymentDetailsDto dto = new PaymentDetailsDto();
		dto.setId(this.getId());
		dto.setIssuerPaymentId(this.getIssuerPaymentId());
		dto.setReferenceId(this.getReferenceId());
		dto.setPaymentDate(DateTimeUtil.toYYYYMMDDDateWithHyphen(this.getPaymentDate()));
		dto.setAdjudicationCutoffDate(DateTimeUtil.toYYYYMMDDDateWithHyphen(this.getAdjudicationCutoffDate()));
		dto.setAdjudicationVersion(this.getAdjudicationVersion());
		dto.setAmount(this.getAmount());
		dto.setRemitMethod(this.getRemitMethod());
		dto.setJurisdictionalBody(this.getJurisdictionalBody());
		dto.setNumberOfAttachedPdfs(this.getNumberOfAttachedPdfs());
		dto.setSettlementTime(this.getSettlementTime());
		dto.setPin(this.getPin());
		return dto;
	}

}
