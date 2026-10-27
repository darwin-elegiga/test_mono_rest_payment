package com.uhg.optum.financials.payments.payer.model;

import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.Assertions.assertThat;

@ExtendWith(MockitoExtension.class)
public class BaseOrganizationEventsDetailsTests {

	@InjectMocks
	BaseOrganizationEventsDetails baseOrganizationEventsDetails;

	@Mock
	Address address;

	@BeforeEach
	void setUp() {
		this.baseOrganizationEventsDetails = new BaseOrganizationEventsDetails();
		this.address = new Address();
		this.address.setLine1("123 Main St");
		this.address.setLine2("Apt 4B");
		this.address.setCity("Springfield");
		this.address.setState("IL");
		this.address.setCountry("us");
		this.address.setPostalCode("62704");
		this.baseOrganizationEventsDetails.setAddress(this.address);
	}

	@Test
	public void testBaseOrganizationEventsDetails() {
		this.baseOrganizationEventsDetails.setCreatedOn(null);
		this.baseOrganizationEventsDetails.setUpdatedOn(null);
		this.baseOrganizationEventsDetails.setCreatedById("createdById");
		this.baseOrganizationEventsDetails.setUpdatedById("updatedById");
		this.baseOrganizationEventsDetails.setTin("123456789");
		this.baseOrganizationEventsDetails.setPayer835Id("123456789");
		this.baseOrganizationEventsDetails.setName("test");
		this.baseOrganizationEventsDetails.setAddress(new Address());
		this.baseOrganizationEventsDetails.setProgramId("test");

		Assertions.assertThat(this.baseOrganizationEventsDetails.getTin()).isNotNull();
		Assertions.assertThat(this.baseOrganizationEventsDetails.getPayer835Id()).isNotNull();
		Assertions.assertThat(this.baseOrganizationEventsDetails.getUpdatedById()).isNotNull();
		Assertions.assertThat(this.baseOrganizationEventsDetails.getName()).isNotNull();
		Assertions.assertThat(this.baseOrganizationEventsDetails.getProgramId()).isNotNull();
		Assertions.assertThat(this.baseOrganizationEventsDetails.getCreatedOn()).isNull();
		Assertions.assertThat(this.baseOrganizationEventsDetails.getAddress()).isNotNull();
		Assertions.assertThat(this.baseOrganizationEventsDetails.getUpdatedOn()).isNull();

	}

	@Test
	void testToDtoWithOrganizationAddress() {
		com.uhg.optum.financials.payments.organization.models.Address addressDto = new com.uhg.optum.financials.payments.organization.models.Address();
		com.uhg.optum.financials.payments.organization.models.Address result = this.baseOrganizationEventsDetails
			.toDto(addressDto);
		assertThat(result).isNotNull();
		assertThat(result.getAddressLine1()).isEqualTo("123 Main St");
		assertThat(result.getAddressLine2()).isEqualTo("Apt 4B");
		assertThat(result.getCity()).isEqualTo("Springfield");
		assertThat(result.getState()).isEqualTo("IL");
		assertThat(result.getPostalCode()).isEqualTo("62704");
	}

}
