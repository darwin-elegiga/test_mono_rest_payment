package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.shared.client.dto.payment.ShippingDetailsDto;
import com.uhg.optum.financials.shared.client.model.BaseShippingDetails;
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
@Table(name = "shipping_details", schema = "virtual_card")
public class ShippingDetails extends BaseShippingDetails {

	@Id
	@GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "virtual_card.shipping_details_id_seq")
	private Long id;

	@OneToOne(cascade = CascadeType.ALL, fetch = FetchType.LAZY)
	private Address address;

	public ShippingDetailsDto toDto() {
		ShippingDetailsDto dto = new ShippingDetailsDto();
		dto.setName(this.getName());
		if (!ObjectUtils.isEmpty(this.address)) {
			dto.setAddress(this.address.toDto());
		}
		return dto;
	}

}
