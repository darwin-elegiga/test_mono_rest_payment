package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.batch.BatchEventDto;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.service.BatchEventPublishService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.stereotype.Service;

import java.time.Instant;

import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@Service
@Slf4j
public class BatchEventPublishServiceImpl implements BatchEventPublishService {

	public static final String BATCH_SENT = "batch_correlated";

	@Autowired
	@Qualifier("kafkaEventBusProducer")
	private EventBusProducer producerService;

	@Autowired
	private LogUtility logUtility;

	@Override
	public String publishBatchEvent(BatchEventDto batchEventDto) {
		try {
			this.logUtility.logMessage("Sending message to Queue", INFO, log);
			EventBusEvent<CorrelationDto> event = convertBatchEventDtoToEventMessage(batchEventDto);
			this.producerService.sendEvent(event, null);
			return "SUCCESS";
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Exception occurred while sending message to event bus: " + ex.getMessage(),
					ERROR, log);
			return "FAILED";
		}
	}

	private EventBusEvent<CorrelationDto> convertBatchEventDtoToEventMessage(BatchEventDto batchEventDto) {
		EventBusEvent<CorrelationDto> eventMessage = new EventBusEvent<>();
		eventMessage.setId(String.valueOf(batchEventDto.getId()));
		eventMessage.setTenantId(String.valueOf(batchEventDto.getTenantId()));
		eventMessage.setEventId(batchEventDto.getEventId());
		eventMessage.setPayload(batchEventDto.getCorrelationDto());
		eventMessage.setType(BATCH_SENT);
		eventMessage.setCreatedOn(Instant.now());
		return eventMessage;
	}

}
