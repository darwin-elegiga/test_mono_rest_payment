package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.CustomRemarkUpdatedEventDto;
import com.uhg.optum.financials.payments.payer.service.CustomRemarksPublisherService;
import com.uhg.optum.financials.payments.remittance.events.CustomRemarkUpdated;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import lombok.NoArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.stereotype.Service;

import java.time.Instant;

@Service
@Slf4j
@NoArgsConstructor
public class CustomRemarksPublisherServiceImpl implements CustomRemarksPublisherService {

	@Autowired
	@Qualifier("kafkaEventBusProducer")
	private EventBusProducer producerService;

	@Override
	public String publishCustomRemarksUpdatedEvent(CustomRemarkUpdatedEventDto customRemarkUpdatedEventDto) {
		try {
			log.info("Sending message to event hub");
			EventBusEvent<CustomRemarkUpdated> event = convertCustomRemarksUpdatedDtoToEventMessage(
					customRemarkUpdatedEventDto);
			this.producerService.sendEvent(event, null);
			return "SUCCESS";
		}
		catch (Exception ex) {
			log.error("Exception occurred while sending message to event bus :{}", ex.getMessage());
			return "FAILED";
		}
	}

	private EventBusEvent<CustomRemarkUpdated> convertCustomRemarksUpdatedDtoToEventMessage(
			CustomRemarkUpdatedEventDto customRemarkUpdatedEventDto) {
		EventBusEvent<CustomRemarkUpdated> eventMessage = new EventBusEvent<>();
		eventMessage.setId(String.valueOf(customRemarkUpdatedEventDto.getId()));
		eventMessage.setTenantId(String.valueOf(customRemarkUpdatedEventDto.getTenantId()));
		eventMessage.setEventId(customRemarkUpdatedEventDto.getEventId());
		eventMessage.setPayload(customRemarkUpdatedEventDto.getCustomRemarkUpdated());
		eventMessage.setType("epra-custom-remark");
		eventMessage.setCreatedOn(Instant.now());
		return eventMessage;
	}

}
