package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.shared.client.dto.payment.ClaimsDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.client.model.BasePaymentRequest;
import jakarta.persistence.CascadeType;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.FetchType;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.OneToOne;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;
import org.springframework.util.ObjectUtils;

@Entity
@Getter
@Setter
@Table(name = "payment_request", schema = "virtual_card")
public class PaymentRequest extends BasePaymentRequest {

	@Id
	@GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "virtual_card.payment_request_id_seq")
	private Long id;

	@Column(length = 36)
	private String payr835Id;

	@Column(length = 50)
	private String submitterId;

	@OneToOne(cascade = CascadeType.ALL, fetch = FetchType.EAGER)
	private PaymentDetails paymentDetails;

	@OneToOne(cascade = CascadeType.ALL, fetch = FetchType.EAGER)
	private PayeeDetails payeeDetails;

	@OneToOne(cascade = CascadeType.ALL, fetch = FetchType.LAZY)
	private ShippingDetails shippingDetails;

	@Column(length = 4, nullable = false)
	private String status;

	@Column(columnDefinition = "TEXT")
	private String stSeSegment;

	@Column(name = "re_preference_flag", nullable = false)
	private boolean rePreferenceFlag;

	@Column(name = "re_preference_processed", nullable = false)
	private boolean rePreferenceProcessed;

	@Column(name = "error_message", columnDefinition = "TEXT")
	private String errorMessage;

	public PaymentRequestDto toDto() {
		PaymentRequestDto dto = new PaymentRequestDto();
		dto.setOrigin(this.getOrigin());
		dto.setOrgShortCode(this.getOrgShortCode());
		dto.setAccountShortCode(this.getAccountShortCode());
		dto.setSubAccountShortCode(this.getSubAccountShortCode());
		dto.setCorrelationId(this.getCorrelationId());
		if (!ObjectUtils.isEmpty(this.paymentDetails)) {
			dto.setPaymentDetails(this.paymentDetails.toDto());
		}
		if (!ObjectUtils.isEmpty(this.payeeDetails)) {
			dto.setPayeeDetails(this.payeeDetails.toDto());
		}
		if (!ObjectUtils.isEmpty(this.shippingDetails)) {
			dto.setShippingDetails(this.shippingDetails.toDto());
		}
		dto.setClaims(new ClaimsDto(this.getTotalClmCount(), this.getClmUrl(), this.getDocType(), this.isPlb()));
		dto.setDocuments(this.getDocuments());
		dto.setMetadata(this.getMetadata());
		dto.setRePreferenced(this.rePreferenceFlag);
		return dto;
	}

}
