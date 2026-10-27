package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.batch.BatchEventDto;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;

import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.isNull;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;

class BatchEventPublishServiceImplTests {

	@InjectMocks
	private BatchEventPublishServiceImpl batchEventPublishService;

	@Mock
	private EventBusProducer producerService;

	@Mock
	private LogUtility logUtility;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void publishBatchEvent_returnsSuccessWhenEventIsPublished() {
		BatchEventDto batchEventDto = new BatchEventDto();
		batchEventDto.setId("12345");
		batchEventDto.setTenantId(UUID.randomUUID());
		batchEventDto.setEventId(UUID.randomUUID());
		batchEventDto.setCorrelationDto(new CorrelationDto());
		String result = this.batchEventPublishService.publishBatchEvent(batchEventDto);
		assertThat(result).isEqualTo("SUCCESS");
		verify(this.producerService, times(1)).sendEvent(any(EventBusEvent.class), isNull());
	}

	@Test
	void publishBatchEvent_returnsFailedWhenExceptionOccurs() {
		BatchEventDto batchEventDto = new BatchEventDto();
		batchEventDto.setId("12345");
		batchEventDto.setTenantId(UUID.randomUUID());
		batchEventDto.setEventId(UUID.randomUUID());
		batchEventDto.setCorrelationDto(new CorrelationDto());
		Mockito.doThrow(RuntimeException.class)
			.when(this.producerService)
			.sendEvent(any(EventBusEvent.class), isNull());
		String result = this.batchEventPublishService.publishBatchEvent(batchEventDto);
		assertThat(result).isEqualTo("FAILED");
		verify(this.producerService, times(1)).sendEvent(any(EventBusEvent.class), isNull());
	}

}
