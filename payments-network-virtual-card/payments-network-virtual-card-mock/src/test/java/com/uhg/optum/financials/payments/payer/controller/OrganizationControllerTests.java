package com.uhg.optum.financials.payments.payer.controller;

import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.willThrow;

import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationUpdatedDto;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.service.OrganizationPublisherService;
import org.assertj.core.api.BDDAssertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

@ExtendWith(MockitoExtension.class)
class OrganizationControllerTests {

	@Mock
	private OrganizationPublisherService organizationPublisherService;

	@InjectMocks
	private OrganizationController organizationController;

	@Test
	void testProduceOrgRegisteredReturnsOk() {
		OrganizationRegisteredEvent event = new OrganizationRegisteredEvent();
		given(this.organizationPublisherService.publishOrganizationRegisteredEvent(event)).willReturn("registered");
		ResponseEntity<?> response = this.organizationController.produceOrgRegistered(event);
		org.assertj.core.api.BDDAssertions.then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		org.assertj.core.api.BDDAssertions.then(response.getBody()).isEqualTo("registered");
	}

	@Test
	void testProduceOrgUpdatedReturnsOk() {
		OrganizationUpdatedDto dto = new OrganizationUpdatedDto();
		given(this.organizationPublisherService.publishOrganizationUpdatedEvent(dto)).willReturn("updated");
		ResponseEntity<?> response = this.organizationController.produceOrgUpdated(dto);
		org.assertj.core.api.BDDAssertions.then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		org.assertj.core.api.BDDAssertions.then(response.getBody()).isEqualTo("updated");
	}

	@Test
	void testGetOrganizationReturnsOk() {
		Organization org = new Organization();
		String orgId = "ORG123";
		given(this.organizationPublisherService.getOrgRegDetails(orgId)).willReturn(org);
		ResponseEntity<?> response = this.organizationController.getOrganization(orgId);
		BDDAssertions.then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		BDDAssertions.then(response.getBody()).isSameAs(org);
	}

	@Test
	void testGetOrganization_whenOrgIdContainsControlChars() {
		String maliciousId = "ORG123\r\ninjected-log-line";
		Organization org = new Organization();
		given(this.organizationPublisherService.getOrgRegDetails(maliciousId)).willReturn(org);
		ResponseEntity<?> response = this.organizationController.getOrganization(maliciousId);
		BDDAssertions.then(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		BDDAssertions.then(response.getBody()).isSameAs(org);
	}

	@Test
	void testGetOrganizationThrowsException() {
		String orgId = "ORG404";
		willThrow(new InternalServerException("Database error occurred while fetching organization details"))
			.given(this.organizationPublisherService)
			.getOrgRegDetails(orgId);
		org.assertj.core.api.Assertions.assertThatThrownBy(() -> this.organizationController.getOrganization(orgId))
			.isInstanceOf(InternalServerException.class)
			.hasMessageContaining("Database error occurred while fetching organization details");
	}

}
