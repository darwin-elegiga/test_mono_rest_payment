package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.events.OrganizationRegistered;
import com.uhg.optum.financials.payments.organization.events.OrganizationUpdated;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationUpdatedDto;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.events.Organization;
import com.uhg.optum.financials.payments.payer.repository.OrganizationRepository;
import com.uhg.optum.financials.payments.payer.service.OrganizationPublisherService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.DEBUG;
import static com.uhg.optum.financials.payments.payer.constant.Constants.FAILED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.ORGANIZATION_REGISTERED_EVENT;
import static com.uhg.optum.financials.payments.payer.constant.Constants.ORGANIZATION_UPDATED_EVENT;
import static com.uhg.optum.financials.payments.payer.constant.Constants.SUCCESS;

@Service
@Slf4j
public class OrganizationPublisherServiceImpl implements OrganizationPublisherService {

	@Autowired
	private EventBusProducer producerService;

	@Autowired
	private OrganizationRepository organizationRepository;

	@Autowired
	LogUtility logUtility;

	public String publishOrganizationRegisteredEvent(OrganizationRegisteredEvent organizationRegisteredEvent) {
		try {
			EventBusEvent<OrganizationRegistered> event = convertOrganizationDtoToEventMessage(
					organizationRegisteredEvent);
			log.info("Sending message to event hub {}", event.getType());
			this.producerService.sendEvent(event, null);
			return SUCCESS;
		}
		catch (Exception ex) {
			log.error("Exception occurred while sending message to event bus", ex);
			return FAILED;
		}
	}

	@Override
	public String publishOrganizationUpdatedEvent(OrganizationUpdatedDto organizationUpdatedDto) {
		try {
			EventBusEvent<OrganizationUpdated> event = convertOrgUpdatedDtoToEventMessage(organizationUpdatedDto);
			log.info("Sending OrgUpdated message to event hub {}", event.getType());
			this.producerService.sendEvent(event, null);
			return SUCCESS;
		}
		catch (Exception ex) {
			log.error("Exception occurred while sending message to event bus", ex);
			return FAILED;
		}
	}

	@Override
	public Organization getOrgRegDetails(String org) {
		try {
			UUID orgId = UUID.fromString(org);
			Organization subAccountRegistrationDetails = this.organizationRepository.findByOrgId(orgId);
			if (subAccountRegistrationDetails != null) {
				return subAccountRegistrationDetails;
			}
			else {
				this.logUtility.logMessage("org reg details not found " + orgId, DEBUG, log);
				throw new NotFoundException("org registration details not found " + orgId);
			}
		}
		catch (Exception ex) {
			log.error("Error while fetching organization details", ex);
			throw new InternalServerException("Database error occurred while fetching organization details");
		}
	}

	private EventBusEvent<OrganizationRegistered> convertOrganizationDtoToEventMessage(
			OrganizationRegisteredEvent organizationRegisteredEvent) {
		EventBusEvent<OrganizationRegistered> eventMessage = new EventBusEvent<>();

		eventMessage.setId(String.valueOf(organizationRegisteredEvent.getId()));
		eventMessage.setTenantId(String.valueOf(organizationRegisteredEvent.getTenantId()));
		eventMessage.setEventId(organizationRegisteredEvent.getEventId());
		eventMessage.setEntityName("virtual-card");
		eventMessage.setSource("Virtual-Card-MOCK");
		eventMessage.setPayload(organizationRegisteredEvent.getOrganizationRegistered());
		eventMessage.setType(ORGANIZATION_REGISTERED_EVENT);
		eventMessage.setCreatedOn(Instant.now());
		return eventMessage;
	}

	private EventBusEvent<OrganizationUpdated> convertOrgUpdatedDtoToEventMessage(
			OrganizationUpdatedDto organizationUpdatedDto) {
		EventBusEvent<OrganizationUpdated> eventMessage = new EventBusEvent<>();

		eventMessage.setId(String.valueOf(organizationUpdatedDto.getId()));
		eventMessage.setTenantId(String.valueOf(organizationUpdatedDto.getTenantId()));
		eventMessage.setEventId(organizationUpdatedDto.getEventId());
		eventMessage.setEntityName("virtual-card");
		eventMessage.setSource("Virtual-Card-MOCK");
		eventMessage.setPayload(organizationUpdatedDto.getOrganizationUpdated());
		eventMessage.setType(ORGANIZATION_UPDATED_EVENT);
		eventMessage.setCreatedOn(Instant.now());
		return eventMessage;
	}

}
