package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.shared.client.dto.payment.PayeeDetailsDto;
import com.uhg.optum.financials.shared.client.model.BasePayeeDetails;
import jakarta.persistence.CascadeType;
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
@Table(name = "payee_details", schema = "virtual_card")
public class PayeeDetails extends BasePayeeDetails {

	@Id
	@GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "virtual_card.payee_details_id_seq")
	private Long id;

	@OneToOne(cascade = CascadeType.ALL, fetch = FetchType.EAGER)
	private Address address;

	public PayeeDetailsDto toDto() {
		PayeeDetailsDto dto = new PayeeDetailsDto();
		dto.setType(this.getType());
		dto.setName(this.getName());
		dto.setTaxId(this.getTaxId());
		dto.setNpi(this.getNpi());
		dto.setReceiverId(this.getReceiverId());
		if (!ObjectUtils.isEmpty(this.address)) {
			dto.setAddress(this.address.toDto());
		}
		return dto;
	}

}
