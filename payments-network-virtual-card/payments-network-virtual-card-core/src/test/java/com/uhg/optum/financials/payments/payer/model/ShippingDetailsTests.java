package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.shared.client.dto.payment.ShippingDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.AddressDto;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.assertj.core.api.Assertions;
import org.mockito.Mockito;

@DisplayName("ShippingDetails Entity Tests")
class ShippingDetailsTests {

	@Test
	@DisplayName("should map all fields and address when address is not null in toDto()")
	void shouldMapAllFieldsAndAddress() {
		// Arrange
		ShippingDetails shippingDetails = new ShippingDetails();
		this.setupShippingDetails(shippingDetails);
		Address address = Mockito.mock(Address.class);
		AddressDto addressDto = Mockito.mock(AddressDto.class);
		Mockito.when(address.toDto()).thenReturn(addressDto);
		shippingDetails.setAddress(address);

		// Act
		ShippingDetailsDto dto = shippingDetails.toDto();

		// Assert
		Assertions.assertThat(dto.getName()).isEqualTo(shippingDetails.getName());
		Assertions.assertThat(dto.getAddress()).isEqualTo(addressDto);
	}

	@Test
	@DisplayName("should map all fields and set address null when address is null in toDto()")
	void shouldMapAllFieldsAndSetAddressNull() {
		// Arrange
		ShippingDetails shippingDetails = new ShippingDetails();
		this.setupShippingDetails(shippingDetails);
		shippingDetails.setAddress(null);

		// Act
		ShippingDetailsDto dto = shippingDetails.toDto();

		// Assert
		Assertions.assertThat(dto.getName()).isEqualTo(shippingDetails.getName());
		Assertions.assertThat(dto.getAddress()).isNull();
	}

	@Test
	@DisplayName("should set and get all fields from BaseShippingDetails and ShippingDetails")
	void shouldSetAndGetAllFields() {
		// Arrange
		ShippingDetails shippingDetails = new ShippingDetails();
		this.setupShippingDetails(shippingDetails);
		shippingDetails.setId(100L);
		Address address = Mockito.mock(Address.class);
		shippingDetails.setAddress(address);

		// Assert
		Assertions.assertThat(shippingDetails.getName()).isEqualTo("Express Shipping");
		Assertions.assertThat(shippingDetails.getId()).isEqualTo(100L);
		Assertions.assertThat(shippingDetails.getAddress()).isEqualTo(address);
	}

	@Test
	@DisplayName("should construct ShippingDetailsDto with all fields")
	void shouldConstructDtoWithAllFields() {
		// Arrange
		AddressDto addressDto = Mockito.mock(AddressDto.class);
		ShippingDetailsDto dto = new ShippingDetailsDto("Express", "Express Shipping", addressDto);

		// Assert
		Assertions.assertThat(dto.getType()).isEqualTo("Express");
		Assertions.assertThat(dto.getName()).isEqualTo("Express Shipping");
		Assertions.assertThat(dto.getAddress()).isEqualTo(addressDto);
	}

	@Test
	@DisplayName("should construct empty ShippingDetailsDto")
	void shouldConstructEmptyDto() {
		// Act
		ShippingDetailsDto dto = new ShippingDetailsDto();

		// Assert
		Assertions.assertThat(dto.getType()).isNull();
		Assertions.assertThat(dto.getName()).isNull();
		Assertions.assertThat(dto.getAddress()).isNull();
	}

	private void setupShippingDetails(ShippingDetails shippingDetails) {
		shippingDetails.setName("Express Shipping");
	}

}
