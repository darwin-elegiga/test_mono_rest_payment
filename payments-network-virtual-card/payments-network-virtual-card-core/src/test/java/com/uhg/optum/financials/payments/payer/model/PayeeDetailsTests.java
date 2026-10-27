package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.shared.client.dto.payment.PayeeDetailsDto;
import com.uhg.optum.financials.shared.client.dto.payment.AddressDto;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.assertj.core.api.Assertions;
import org.mockito.Mockito;
import java.time.LocalDateTime;

@DisplayName("PayeeDetails Entity Tests")
class PayeeDetailsTests {

	@Test
	@DisplayName("should map all fields and address when address is not null")
	void shouldMapAllFieldsAndAddress() {
		// Arrange
		PayeeDetails payeeDetails = new PayeeDetails();
		this.setupPayeeDetails(payeeDetails);
		Address address = Mockito.mock(Address.class);
		AddressDto addressDto = Mockito.mock(AddressDto.class);
		Mockito.when(address.toDto()).thenReturn(addressDto);
		payeeDetails.setAddress(address);

		// Act
		PayeeDetailsDto dto = payeeDetails.toDto();

		// Assert
		Assertions.assertThat(dto.getType()).isEqualTo(payeeDetails.getType());
		Assertions.assertThat(dto.getName()).isEqualTo(payeeDetails.getName());
		Assertions.assertThat(dto.getTaxId()).isEqualTo(payeeDetails.getTaxId());
		Assertions.assertThat(dto.getNpi()).isEqualTo(payeeDetails.getNpi());
		Assertions.assertThat(dto.getReceiverId()).isEqualTo(payeeDetails.getReceiverId());
		Assertions.assertThat(dto.getAddress()).isEqualTo(addressDto);
	}

	@Test
	@DisplayName("should map all fields and set address null when address is null")
	void shouldMapAllFieldsAndSetAddressNull() {
		// Arrange
		PayeeDetails payeeDetails = new PayeeDetails();
		this.setupPayeeDetails(payeeDetails);
		payeeDetails.setAddress(null);

		// Act
		PayeeDetailsDto dto = payeeDetails.toDto();

		// Assert
		Assertions.assertThat(dto.getType()).isEqualTo(payeeDetails.getType());
		Assertions.assertThat(dto.getName()).isEqualTo(payeeDetails.getName());
		Assertions.assertThat(dto.getTaxId()).isEqualTo(payeeDetails.getTaxId());
		Assertions.assertThat(dto.getNpi()).isEqualTo(payeeDetails.getNpi());
		Assertions.assertThat(dto.getReceiverId()).isEqualTo(payeeDetails.getReceiverId());
		Assertions.assertThat(dto.getAddress()).isNull();
	}

	@Test
	@DisplayName("should set and get all fields from BaseEntity and BasePayeeDetails")
	void shouldSetAndGetAllFields() {
		// Arrange
		PayeeDetails payeeDetails = new PayeeDetails();
		LocalDateTime now = LocalDateTime.now();
		payeeDetails.setCreatedById("creator");
		payeeDetails.setCreatedOn(now);
		payeeDetails.setUpdatedById("updator");
		payeeDetails.setUpdatedOn(now);
		payeeDetails.setType("A");
		payeeDetails.setName("Test Name");
		payeeDetails.setTaxId("123456789");
		payeeDetails.setNpi("9876543210");
		payeeDetails.setReceiverId("receiver");
		payeeDetails.setId(1L);
		Address address = Mockito.mock(Address.class);
		payeeDetails.setAddress(address);

		// Assert
		Assertions.assertThat(payeeDetails.getCreatedById()).isEqualTo("creator");
		Assertions.assertThat(payeeDetails.getCreatedOn()).isEqualTo(now);
		Assertions.assertThat(payeeDetails.getUpdatedById()).isEqualTo("updator");
		Assertions.assertThat(payeeDetails.getUpdatedOn()).isEqualTo(now);
		Assertions.assertThat(payeeDetails.getType()).isEqualTo("A");
		Assertions.assertThat(payeeDetails.getName()).isEqualTo("Test Name");
		Assertions.assertThat(payeeDetails.getTaxId()).isEqualTo("123456789");
		Assertions.assertThat(payeeDetails.getNpi()).isEqualTo("9876543210");
		Assertions.assertThat(payeeDetails.getReceiverId()).isEqualTo("receiver");
		Assertions.assertThat(payeeDetails.getId()).isEqualTo(1L);
		Assertions.assertThat(payeeDetails.getAddress()).isEqualTo(address);
	}

	@Test
	@DisplayName("@PrePersist should set createdOn and updatedOn")
	void prePersistShouldSetDates() {
		// Arrange
		PayeeDetails payeeDetails = new PayeeDetails();

		// Act
		payeeDetails.setCreationDateTime();

		// Assert
		Assertions.assertThat(payeeDetails.getCreatedOn()).isNotNull();
		Assertions.assertThat(payeeDetails.getUpdatedOn()).isNotNull();
	}

	@Test
	@DisplayName("@PreUpdate should update updatedOn")
	void preUpdateShouldUpdateDate() {
		// Arrange
		PayeeDetails payeeDetails = new PayeeDetails();
		payeeDetails.setCreationDateTime();
		LocalDateTime beforeUpdate = payeeDetails.getUpdatedOn();

		// Act
		payeeDetails.setChangeDateTime();

		// Assert
		Assertions.assertThat(payeeDetails.getUpdatedOn()).isAfterOrEqualTo(beforeUpdate);
	}

	@Test
	@DisplayName("should construct PayeeDetailsDto with all fields")
	void shouldConstructDtoWithAllFields() {
		// Arrange
		AddressDto addressDto = new AddressDto();

		// Act
		PayeeDetailsDto dto = new PayeeDetailsDto("B", "John Doe", addressDto, "123456789", "1234567890", "RCV123456");

		// Assert
		Assertions.assertThat(dto.getType()).isEqualTo("B");
		Assertions.assertThat(dto.getName()).isEqualTo("John Doe");
		Assertions.assertThat(dto.getAddress()).isEqualTo(addressDto);
		Assertions.assertThat(dto.getTaxId()).isEqualTo("123456789");
		Assertions.assertThat(dto.getNpi()).isEqualTo("1234567890");
		Assertions.assertThat(dto.getReceiverId()).isEqualTo("RCV123456");
	}

	@Test
	@DisplayName("should construct empty PayeeDetailsDto")
	void shouldConstructEmptyDto() {
		// Act
		PayeeDetailsDto dto = new PayeeDetailsDto();

		// Assert
		Assertions.assertThat(dto.getType()).isNull();
		Assertions.assertThat(dto.getName()).isNull();
		Assertions.assertThat(dto.getAddress()).isNull();
		Assertions.assertThat(dto.getTaxId()).isNull();
		Assertions.assertThat(dto.getNpi()).isNull();
		Assertions.assertThat(dto.getReceiverId()).isNull();
	}

	private void setupPayeeDetails(PayeeDetails payeeDetails) {
		payeeDetails.setType("A");
		payeeDetails.setName("Test Name");
		payeeDetails.setTaxId("123456789");
		payeeDetails.setNpi("9876543210");
		payeeDetails.setReceiverId("receiver");
	}

}
