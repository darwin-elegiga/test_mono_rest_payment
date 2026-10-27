package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.events.OrganizationRegistered;
import com.uhg.optum.financials.payments.organization.events.OrganizationUpdated;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationUpdatedDto;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.UUID;

import static org.assertj.core.api.BDDAssertions.then;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.willThrow;
import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
class OrganizationPublisherServiceImplTests {

	@Mock
	private EventBusProducer producerService;

	@Mock
	private OrganizationRepository organizationRepository;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private OrganizationPublisherServiceImpl organizationPublisherServiceImpl;

	private OrganizationRegisteredEvent orgRegisteredEvent;

	private OrganizationUpdatedDto orgUpdatedDto;

	private Organization organization;

	@BeforeEach
	void setUp() {
		this.orgRegisteredEvent = new OrganizationRegisteredEvent();
		this.orgRegisteredEvent.setId(UUID.randomUUID());
		this.orgRegisteredEvent.setTenantId(UUID.randomUUID());
		this.orgRegisteredEvent.setEventId(UUID.randomUUID());
		this.orgRegisteredEvent.setOrganizationRegistered(new OrganizationRegistered());

		this.orgUpdatedDto = new OrganizationUpdatedDto();
		this.orgUpdatedDto.setId(UUID.randomUUID());
		this.orgUpdatedDto.setTenantId(UUID.randomUUID());
		this.orgUpdatedDto.setEventId(UUID.randomUUID());
		this.orgUpdatedDto.setOrganizationUpdated(new OrganizationUpdated());

		this.organization = new Organization();
	}

	@Test
	void publishOrganizationRegisteredEvent_success() {
		String result = this.organizationPublisherServiceImpl
			.publishOrganizationRegisteredEvent(this.orgRegisteredEvent);
		then(result).isEqualTo("success");
		org.mockito.BDDMockito.then(this.producerService).should().sendEvent(any(EventBusEvent.class), any());
	}

	@Test
	void publishOrganizationRegisteredEvent_failure() {
		willThrow(new RuntimeException("fail")).given(this.producerService).sendEvent(any(EventBusEvent.class), any());
		String result = this.organizationPublisherServiceImpl
			.publishOrganizationRegisteredEvent(this.orgRegisteredEvent);
		then(result).isEqualTo("Failed");
	}

	@Test
	void publishOrganizationUpdatedEvent_success() {
		String result = this.organizationPublisherServiceImpl.publishOrganizationUpdatedEvent(this.orgUpdatedDto);
		then(result).isEqualTo("success");
		org.mockito.BDDMockito.then(this.producerService).should().sendEvent(any(EventBusEvent.class), any());
	}

	@Test
	void publishOrganizationUpdatedEvent_failure() {
		willThrow(new RuntimeException("fail")).given(this.producerService).sendEvent(any(EventBusEvent.class), any());
		String result = this.organizationPublisherServiceImpl.publishOrganizationUpdatedEvent(this.orgUpdatedDto);
		then(result).isEqualTo("Failed");
	}

	@Test
	void getOrgRegDetails_found() {
		UUID id = UUID.randomUUID();
		given(this.organizationRepository.findByOrgId(id)).willReturn(this.organization);
		Organization result = this.organizationPublisherServiceImpl.getOrgRegDetails(id.toString());
		then(result).isEqualTo(this.organization);
	}

	@Test
	void getOrgRegDetails_notFound() {
		UUID id = UUID.randomUUID();
		given(this.organizationRepository.findByOrgId(id)).willReturn(null);
		assertThatThrownBy(() -> this.organizationPublisherServiceImpl.getOrgRegDetails(id.toString()))
			.isInstanceOf(InternalServerException.class)
			.hasMessageContaining("Database error occurred while fetching organization details");
		org.mockito.BDDMockito.then(this.logUtility)
			.should()
			.logMessage(org.mockito.ArgumentMatchers.contains("org reg details not found"), any(), any());
	}

	@Test
	void convertOrganizationDtoToEventMessage_shouldMapAllFields() throws Exception {
		java.lang.reflect.Method method = OrganizationPublisherServiceImpl.class
			.getDeclaredMethod("convertOrganizationDtoToEventMessage", OrganizationRegisteredEvent.class);
		method.setAccessible(true);
		EventBusEvent<OrganizationRegistered> event = (EventBusEvent<OrganizationRegistered>) method
			.invoke(this.organizationPublisherServiceImpl, this.orgRegisteredEvent);
		then(event.getId()).isEqualTo(this.orgRegisteredEvent.getId().toString());
		then(event.getTenantId()).isEqualTo(this.orgRegisteredEvent.getTenantId().toString());
		then(event.getEventId()).isEqualTo(this.orgRegisteredEvent.getEventId());
		then(event.getPayload()).isEqualTo(this.orgRegisteredEvent.getOrganizationRegistered());
		then(event.getType()).isEqualTo("organization_registered");
		then(event.getEntityName()).isEqualTo("virtual-card");
		then(event.getSource()).isEqualTo("Virtual-Card-MOCK");
		then(event.getCreatedOn()).isNotNull();
	}

	@Test
	void convertOrgUpdatedDtoToEventMessage_shouldMapAllFields() throws Exception {
		java.lang.reflect.Method method = OrganizationPublisherServiceImpl.class
			.getDeclaredMethod("convertOrgUpdatedDtoToEventMessage", OrganizationUpdatedDto.class);
		method.setAccessible(true);
		EventBusEvent<OrganizationUpdated> event = (EventBusEvent<OrganizationUpdated>) method
			.invoke(this.organizationPublisherServiceImpl, this.orgUpdatedDto);
		then(event.getId()).isEqualTo(this.orgUpdatedDto.getId().toString());
		then(event.getTenantId()).isEqualTo(this.orgUpdatedDto.getTenantId().toString());
		then(event.getEventId()).isEqualTo(this.orgUpdatedDto.getEventId());
		then(event.getPayload()).isEqualTo(this.orgUpdatedDto.getOrganizationUpdated());
		then(event.getType()).isEqualTo("organization_updated");
		then(event.getEntityName()).isEqualTo("virtual-card");
		then(event.getSource()).isEqualTo("Virtual-Card-MOCK");
		then(event.getCreatedOn()).isNotNull();
	}

}
