package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.shared.client.dto.payment.AddressDto;
import com.uhg.optum.financials.shared.client.model.BaseAddress;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.SequenceGenerator;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.Setter;

@Entity
@Getter
@Setter
@Table(name = "address", schema = "virtual_card")
public class Address extends BaseAddress {

	@Id
	@GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "virtual_card.address_id_seq")
	@SequenceGenerator(name = "virtual_card.address_id_seq", allocationSize = 1)
	private Long id;

	public AddressDto toDto() {
		AddressDto addressDto = new AddressDto();
		addressDto.setLine1(this.getLine1());
		addressDto.setLine2(this.getLine2());
		addressDto.setCity(this.getCity());
		addressDto.setState(this.getState());
		addressDto.setCountry(this.getCountry());
		addressDto.setPostalCode(this.getPostalCode());
		return addressDto;
	}

}
