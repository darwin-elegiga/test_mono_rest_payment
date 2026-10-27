package com.uhg.optum.financials.payments.payer.model;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;

@ExtendWith(MockitoExtension.class)
class AddressTests {

	@InjectMocks
	Address address;

	@Test
	void testProperty() {

		this.address.setId(1L);
		this.address.setLine1("line1");
		this.address.setLine2("line2");
		this.address.setCity("city");
		this.address.setState("state");
		this.address.setCountry("country");
		this.address.setPostalCode("12345");

		assertThat(this.address.getId()).isEqualTo(1L);
		assertThat(this.address.getLine1()).isEqualTo("line1");
		assertThat(this.address.getLine2()).isEqualTo("line2");
		assertThat(this.address.getCity()).isEqualTo("city");
		assertThat(this.address.getState()).isEqualTo("state");
		assertThat(this.address.getCountry()).isEqualTo("country");
		assertThat(this.address.getPostalCode()).isEqualTo("12345");
		assertThat(this.address.toDto()).isNotNull();
	}

}
