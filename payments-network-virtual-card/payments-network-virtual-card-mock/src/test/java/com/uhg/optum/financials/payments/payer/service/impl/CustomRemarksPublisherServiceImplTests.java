package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.CustomRemarkUpdatedEventDto;
import com.uhg.optum.financials.payments.remittance.events.CustomRemarkUpdated;
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

class CustomRemarksPublisherServiceImplTests {

	@InjectMocks
	private CustomRemarksPublisherServiceImpl customRemarksPublisherService;

	@Mock
	private EventBusProducer producerService;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void publishCustomRemarksUpdatedEvent_returnsSuccessWhenEventIsPublished() {
		CustomRemarkUpdatedEventDto dto = buildDto();

		String result = this.customRemarksPublisherService.publishCustomRemarksUpdatedEvent(dto);

		assertThat(result).isEqualTo("SUCCESS");
		verify(this.producerService, times(1)).sendEvent(any(EventBusEvent.class), isNull());
	}

	@Test
	void publishCustomRemarksUpdatedEvent_returnsFailedWhenExceptionOccurs() {
		CustomRemarkUpdatedEventDto dto = buildDto();
		Mockito.doThrow(RuntimeException.class)
			.when(this.producerService)
			.sendEvent(any(EventBusEvent.class), isNull());

		String result = this.customRemarksPublisherService.publishCustomRemarksUpdatedEvent(dto);

		assertThat(result).isEqualTo("FAILED");
		verify(this.producerService, times(1)).sendEvent(any(EventBusEvent.class), isNull());
	}

	@Test
	void publishCustomRemarksUpdatedEvent_mapsIdAndTenantIdFromDto() {
		UUID id = UUID.randomUUID();
		UUID tenantId = UUID.randomUUID();
		UUID eventId = UUID.randomUUID();

		CustomRemarkUpdatedEventDto dto = new CustomRemarkUpdatedEventDto();
		dto.setId(id);
		dto.setTenantId(tenantId);
		dto.setEventId(eventId);
		dto.setCustomRemarkUpdated(new CustomRemarkUpdated());

		Mockito.doAnswer((invocation) -> {
			EventBusEvent<?> event = invocation.getArgument(0);
			assertThat(event.getId()).isEqualTo(id.toString());
			assertThat(event.getTenantId()).isEqualTo(tenantId.toString());
			assertThat(event.getEventId()).isEqualTo(eventId);
			assertThat(event.getType()).isEqualTo("epra-custom-remark");
			return null;
		}).when(this.producerService).sendEvent(any(EventBusEvent.class), isNull());

		String result = this.customRemarksPublisherService.publishCustomRemarksUpdatedEvent(dto);

		assertThat(result).isEqualTo("SUCCESS");
	}

	@Test
	void publishCustomRemarksUpdatedEvent_handlesNullCustomRemarkUpdated() {
		CustomRemarkUpdatedEventDto dto = new CustomRemarkUpdatedEventDto();
		dto.setId(UUID.randomUUID());
		dto.setTenantId(UUID.randomUUID());
		dto.setEventId(UUID.randomUUID());
		dto.setCustomRemarkUpdated(null);

		String result = this.customRemarksPublisherService.publishCustomRemarksUpdatedEvent(dto);

		assertThat(result).isEqualTo("SUCCESS");
		verify(this.producerService, times(1)).sendEvent(any(EventBusEvent.class), isNull());
	}

	private CustomRemarkUpdatedEventDto buildDto() {
		CustomRemarkUpdatedEventDto dto = new CustomRemarkUpdatedEventDto();
		dto.setId(UUID.randomUUID());
		dto.setTenantId(UUID.randomUUID());
		dto.setEventId(UUID.randomUUID());
		dto.setCustomRemarkUpdated(new CustomRemarkUpdated());
		return dto;
	}

}
