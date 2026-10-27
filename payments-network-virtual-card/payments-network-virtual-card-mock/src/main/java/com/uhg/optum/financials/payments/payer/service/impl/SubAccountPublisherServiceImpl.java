package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.organization.events.SubAccountUpdated;
import com.uhg.optum.financials.payments.payer.dto.SubAccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.SubAccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.SubAccountPublisherService;
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
import static com.uhg.optum.financials.payments.payer.constant.Constants.SUB_ACCOUNT_REGISTERED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.SUB_ACCOUNT_UPDATED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.SUCCESS;

@Service
@Slf4j
public class SubAccountPublisherServiceImpl implements SubAccountPublisherService {

	@Autowired
	private EventBusProducer producerService;

	@Autowired
	private LogUtility logUtility;

	@Autowired
	private SubAccountRepository subAccountRepository;

	public String publishSubAccountRegisteredEvent(SubAccountRegisteredEvent subAccountRegisteredEvent) {
		try {
			EventBusEvent<SubAccountRegistered> event = convertSubAccountRegisteredDtoToEvent(
					subAccountRegisteredEvent);
			log.info("Sending sub-account registered message to event hub {}", event.getType());
			this.producerService.sendEvent(event, null);
			return SUCCESS;
		}
		catch (Exception ex) {
			log.error("Exception occurred while sending message to event bus :{}", ex.getMessage());
			return FAILED;
		}
	}

	@Override
	public String publishSubAccountUpdatedEvent(SubAccountUpdatedDto subAccountUpdatedDto) {
		try {
			EventBusEvent<SubAccountUpdated> event = convertSubAccountUpdatedDtoToEvent(subAccountUpdatedDto);
			log.info("Sending sub-account updated message to event hub {}", event.getType());
			this.producerService.sendEvent(event, null);
			return SUCCESS;
		}
		catch (Exception ex) {
			log.error("Exception occurred while sending message to event bus :{}", ex.getMessage());
			return FAILED;
		}
	}

	@Override
	public SubAccount getSubAccountRegDetails(String subAccount) {
		UUID subAccountId = UUID.fromString(subAccount);
		SubAccount subAccountRegistrationDetails = this.subAccountRepository.findBySubAccountId(subAccountId);
		if (subAccountRegistrationDetails != null) {
			return subAccountRegistrationDetails;
		}
		else {
			this.logUtility.logMessage("SubAccount reg details not found " + subAccountId, DEBUG, log);
			throw new NotFoundException("SubAccount registration details not found " + subAccountId);
		}
	}

	private EventBusEvent<SubAccountRegistered> convertSubAccountRegisteredDtoToEvent(
			SubAccountRegisteredEvent subAccountRegisteredEvent) {
		EventBusEvent<SubAccountRegistered> eventMessage = new EventBusEvent<>();
		eventMessage.setId(String.valueOf(subAccountRegisteredEvent.getId()));
		eventMessage.setTenantId(String.valueOf(subAccountRegisteredEvent.getTenantId()));
		eventMessage.setEventId(subAccountRegisteredEvent.getEventId());
		eventMessage.setPayload(subAccountRegisteredEvent.getSubAccountRegistered());
		eventMessage.setType(SUB_ACCOUNT_REGISTERED);
		eventMessage.setEntityName("virtual-card");
		eventMessage.setSource("Virtual-Card-MOCK");
		eventMessage.setCreatedOn(Instant.now());
		return eventMessage;
	}

	private EventBusEvent<SubAccountUpdated> convertSubAccountUpdatedDtoToEvent(
			SubAccountUpdatedDto subAccountUpdatedDto) {
		EventBusEvent<SubAccountUpdated> eventMessage = new EventBusEvent<>();
		eventMessage.setId(String.valueOf(subAccountUpdatedDto.getId()));
		eventMessage.setTenantId(String.valueOf(subAccountUpdatedDto.getTenantId()));
		eventMessage.setEventId(subAccountUpdatedDto.getEventId());
		eventMessage.setPayload(subAccountUpdatedDto.getSubAccountUpdated());
		eventMessage.setType(SUB_ACCOUNT_UPDATED);
		eventMessage.setEntityName("virtual-card");
		eventMessage.setSource("Virtual-Card-MOCK");
		eventMessage.setCreatedOn(Instant.now());
		return eventMessage;
	}

}
