package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.organization.events.SubAccountUpdated;
import com.uhg.optum.financials.payments.payer.dto.SubAccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.SubAccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
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
class SubAccountPublisherServiceImplTests {

	@Mock
	private EventBusProducer producerService;

	@Mock
	private SubAccountRepository subAccountRepository;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private SubAccountPublisherServiceImpl subAccountPublisherServiceImpl;

	private SubAccountRegisteredEvent subAccountRegisteredEvent;

	private SubAccountUpdatedDto subAccountUpdatedDto;

	private SubAccount subAccount;

	@BeforeEach
	void setUp() {
		this.subAccountRegisteredEvent = new SubAccountRegisteredEvent();
		this.subAccountRegisteredEvent.setId(UUID.randomUUID());
		this.subAccountRegisteredEvent.setTenantId(UUID.randomUUID());
		this.subAccountRegisteredEvent.setEventId(UUID.randomUUID());
		this.subAccountRegisteredEvent.setSubAccountRegistered(new SubAccountRegistered());

		this.subAccountUpdatedDto = new SubAccountUpdatedDto();
		this.subAccountUpdatedDto.setId(UUID.randomUUID());
		this.subAccountUpdatedDto.setTenantId(UUID.randomUUID());
		this.subAccountUpdatedDto.setEventId(UUID.randomUUID());
		this.subAccountUpdatedDto.setSubAccountUpdated(new SubAccountUpdated());

		this.subAccount = new SubAccount();
	}

	@Test
	void publishSubAccountRegisteredEvent_success() {
		String result = this.subAccountPublisherServiceImpl
			.publishSubAccountRegisteredEvent(this.subAccountRegisteredEvent);
		then(result).isEqualTo("success");
		org.mockito.BDDMockito.then(this.producerService).should().sendEvent(any(EventBusEvent.class), any());
	}

	@Test
	void publishSubAccountRegisteredEvent_failure() {
		willThrow(new RuntimeException("fail")).given(this.producerService).sendEvent(any(EventBusEvent.class), any());
		String result = this.subAccountPublisherServiceImpl
			.publishSubAccountRegisteredEvent(this.subAccountRegisteredEvent);
		then(result).isEqualTo("Failed");
	}

	@Test
	void publishSubAccountUpdatedEvent_success() {
		String result = this.subAccountPublisherServiceImpl.publishSubAccountUpdatedEvent(this.subAccountUpdatedDto);
		then(result).isEqualTo("success");
		org.mockito.BDDMockito.then(this.producerService).should().sendEvent(any(EventBusEvent.class), any());
	}

	@Test
	void publishSubAccountUpdatedEvent_failure() {
		willThrow(new RuntimeException("fail")).given(this.producerService).sendEvent(any(EventBusEvent.class), any());
		String result = this.subAccountPublisherServiceImpl.publishSubAccountUpdatedEvent(this.subAccountUpdatedDto);
		then(result).isEqualTo("Failed");
	}

	@Test
	void getSubAccountRegDetails_found() {
		UUID id = UUID.randomUUID();
		given(this.subAccountRepository.findBySubAccountId(id)).willReturn(this.subAccount);
		SubAccount result = this.subAccountPublisherServiceImpl.getSubAccountRegDetails(id.toString());
		then(result).isEqualTo(this.subAccount);
	}

	@Test
	void getSubAccountRegDetails_notFound() {
		UUID id = UUID.randomUUID();
		given(this.subAccountRepository.findBySubAccountId(id)).willReturn(null);
		assertThatThrownBy(() -> this.subAccountPublisherServiceImpl.getSubAccountRegDetails(id.toString()))
			.isInstanceOf(NotFoundException.class);
		org.mockito.BDDMockito.then(this.logUtility)
			.should()
			.logMessage(org.mockito.ArgumentMatchers.contains("SubAccount reg details not found"), any(), any());
	}

	@Test
	void convertSubAccountRegisteredDtoToEvent_shouldMapAllFields() throws Exception {
		java.lang.reflect.Method method = SubAccountPublisherServiceImpl.class
			.getDeclaredMethod("convertSubAccountRegisteredDtoToEvent", SubAccountRegisteredEvent.class);
		method.setAccessible(true);
		EventBusEvent<SubAccountRegistered> event = (EventBusEvent<SubAccountRegistered>) method
			.invoke(this.subAccountPublisherServiceImpl, this.subAccountRegisteredEvent);
		then(event.getId()).isEqualTo(this.subAccountRegisteredEvent.getId().toString());
		then(event.getTenantId()).isEqualTo(this.subAccountRegisteredEvent.getTenantId().toString());
		then(event.getEventId()).isEqualTo(this.subAccountRegisteredEvent.getEventId());
		then(event.getPayload()).isEqualTo(this.subAccountRegisteredEvent.getSubAccountRegistered());
		then(event.getType()).isEqualTo("sub_account_registered");
		then(event.getEntityName()).isEqualTo("virtual-card");
		then(event.getSource()).isEqualTo("Virtual-Card-MOCK");
		then(event.getCreatedOn()).isNotNull();
	}

	@Test
	void convertSubAccountUpdatedDtoToEvent_shouldMapAllFields() throws Exception {
		java.lang.reflect.Method method = SubAccountPublisherServiceImpl.class
			.getDeclaredMethod("convertSubAccountUpdatedDtoToEvent", SubAccountUpdatedDto.class);
		method.setAccessible(true);
		EventBusEvent<SubAccountUpdated> event = (EventBusEvent<SubAccountUpdated>) method
			.invoke(this.subAccountPublisherServiceImpl, this.subAccountUpdatedDto);
		then(event.getId()).isEqualTo(this.subAccountUpdatedDto.getId().toString());
		then(event.getTenantId()).isEqualTo(this.subAccountUpdatedDto.getTenantId().toString());
		then(event.getEventId()).isEqualTo(this.subAccountUpdatedDto.getEventId());
		then(event.getPayload()).isEqualTo(this.subAccountUpdatedDto.getSubAccountUpdated());
		then(event.getType()).isEqualTo("sub_account_updated");
		then(event.getEntityName()).isEqualTo("virtual-card");
		then(event.getSource()).isEqualTo("Virtual-Card-MOCK");
		then(event.getCreatedOn()).isNotNull();
	}

}
