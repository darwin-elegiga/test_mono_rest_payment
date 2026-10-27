package com.uhg.optum.financials.payments.payer.consumer;

import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.organization.events.OrganizationClosed;
import com.uhg.optum.financials.payments.organization.events.OrganizationRegistered;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.service.CorrelationEventPublisher;
import com.uhg.optum.financials.payments.payer.service.IEventBusHandler;
import com.uhg.optum.financials.payments.payer.service.IEventHandlerRetryExecutorService;
import com.uhg.optum.financials.payments.payer.service.impl.BankAccountEventHandler;
import com.uhg.optum.financials.payments.payer.service.impl.OrganizationEventServiceImpl;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.client.events.IEvent;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;

import static org.mockito.BDDMockito.then;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.times;

@ExtendWith(MockitoExtension.class)
class EventsConsumerTests {

	@InjectMocks
	private EventsConsumer eventConsumer;

	@Mock(name = "organizationEventHandler")
	private OrganizationEventServiceImpl organizationEventHandler;

	@Mock(name = "bankAccountEventHandler")
	private BankAccountEventHandler bankAccountEventHandler;

	@Mock
	private BatchDetailService batchDetailService;

	@Mock
	private CorrelationEventPublisher correlationEventPublisher;

	@Mock
	private IEventHandlerRetryExecutorService iEventHandlerRetryExecutorService;

	@Mock(name = "logoServiceEventHandler")
	private IEventBusHandler logoServiceEventHandler;

	@Mock(name = "localBankAccountEventHandler")
	private IEventBusHandler localBankAccountEventHandler;

	@BeforeEach
	void setUp() throws Exception {
		// Using lenient stubbing to avoid UnnecessaryStubbingException for tests that
		// don't use handlers
		org.mockito.Mockito.lenient().doAnswer((invocation) -> {
			EventBusEvent<? extends IEvent> event = invocation.getArgument(0);
			java.util.function.Consumer<EventBusEvent<? extends IEvent>> handler = invocation.getArgument(2);
			java.util.function.Consumer<EventBusEvent<? extends IEvent>> unknownHandler = invocation.getArgument(3);

			if (handler != null) {
				handler.accept(event);
			}
			else {
				unknownHandler.accept(event);
			}
			return null;
		}).when(this.iEventHandlerRetryExecutorService).callEventHandlersWithRetry(any(), anyString(), any(), any());
	}

	@Test
	@DisplayName("should not interact with handlers when event is null")
	void givenNullEvent_whenConsumeEvent_thenNoInteractions() {
		// Given
		EventBusEvent<IEvent> event = null;
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.organizationEventHandler).shouldHaveNoInteractions();
		then(this.bankAccountEventHandler).shouldHaveNoInteractions();
	}

	@Test
	@DisplayName("should not interact with handlers when event payload is null")
	void givenNullPayload_whenConsumeEvent_thenNoInteractions() {
		// Given
		EventBusEvent<IEvent> event = new EventBusEvent<>();
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.organizationEventHandler).shouldHaveNoInteractions();
		then(this.bankAccountEventHandler).shouldHaveNoInteractions();
	}

	@Test
	@DisplayName("should not interact with handlers when event type is empty")
	void givenEmptyType_whenConsumeEvent_thenNoInteractions() {
		// Given
		IEvent payload = mock(IEvent.class);
		given(payload.type()).willReturn("");
		EventBusEvent<IEvent> event = new EventBusEvent<>();
		event.setPayload(payload);
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.organizationEventHandler).shouldHaveNoInteractions();
		then(this.bankAccountEventHandler).shouldHaveNoInteractions();
	}

	@Test
	@DisplayName("should call organizationEventHandler for SubAccountRegistered event")
	void givenSubAccountRegistered_whenConsumeEvent_thenOrganizationHandlerCalled() {
		// Given
		SubAccountRegistered subAccountRegisteredDto = mock(SubAccountRegistered.class);
		given(subAccountRegisteredDto.type()).willReturn("sub_account_registered");
		EventBusEvent<SubAccountRegistered> event = new EventBusEvent<>();
		event.setPayload(subAccountRegisteredDto);
		event.setId(UUID.randomUUID().toString());
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.organizationEventHandler).should(times(1)).handle(event);
	}

	@Test
	@DisplayName("should call organizationEventHandler for OrganizationRegistered event")
	void givenOrganizationRegistered_whenConsumeEvent_thenOrganizationHandlerCalled() {
		// Given
		OrganizationRegistered dto = mock(OrganizationRegistered.class);
		given(dto.type()).willReturn("organization_registered");
		EventBusEvent<OrganizationRegistered> event = new EventBusEvent<>();
		event.setPayload(dto);
		event.setId(UUID.randomUUID().toString());
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.organizationEventHandler).should(times(1)).handle(event);
	}

	@Test
	@DisplayName("should call organizationEventHandler for AccountRegistered event")
	void givenAccountRegistered_whenConsumeEvent_thenOrganizationHandlerCalled() {
		// Given
		AccountRegistered dto = mock(AccountRegistered.class);
		given(dto.type()).willReturn("account_registered");
		EventBusEvent<AccountRegistered> event = new EventBusEvent<>();
		event.setPayload(dto);
		event.setId(UUID.randomUUID().toString());
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.organizationEventHandler).should(times(1)).handle(event);
	}

	@Test
	@DisplayName("should not interact with handlers for unknown event type")
	void givenUnknownEventType_whenConsumeEvent_thenNoInteractions() {
		// Given
		IEvent unknownEvent = mock(IEvent.class);
		given(unknownEvent.type()).willReturn("UNKNOWN_EVENT");
		EventBusEvent<IEvent> event = new EventBusEvent<>();
		event.setPayload(unknownEvent);
		event.setId(UUID.randomUUID().toString());
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.organizationEventHandler).shouldHaveNoInteractions();
		then(this.bankAccountEventHandler).shouldHaveNoInteractions();
	}

	@Test
	@DisplayName("should call bankAccountEventHandler for BANK_ACCOUNT_CREATED event")
	void givenBankAccountCreated_whenConsumeEvent_thenBankAccountHandlerCalled() {
		// Given
		IEvent bankAccountCreated = mock(IEvent.class);
		given(bankAccountCreated.type()).willReturn("bank_account_created");
		EventBusEvent<IEvent> event = new EventBusEvent<>();
		event.setPayload(bankAccountCreated);
		event.setId(UUID.randomUUID().toString());
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.bankAccountEventHandler).should(times(1)).handle(event);
	}

	@Test
	@DisplayName("should call organizationEventHandler for OrganizationClosed event")
	void givenOrganizationClosed_whenConsumeEvent_thenOrganizationHandlerCalled() {
		// Given
		OrganizationClosed dto = mock(OrganizationClosed.class);
		given(dto.type()).willReturn("organization_closed");
		EventBusEvent<OrganizationClosed> event = new EventBusEvent<>();
		event.setPayload(dto);
		event.setId(UUID.randomUUID().toString());
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.organizationEventHandler).should(times(1)).handle(event);
	}

	@Test
	@DisplayName("should call batchDetailService and correlationEventPublisher for BATCH_CORRELATED event")
	void givenBatchCorrelated_whenConsumeEvent_thenBatchDetailServiceAndCorrelationEventPublisherCalled() {
		// Given
		CorrelationDto dto = mock(CorrelationDto.class);
		given(dto.type()).willReturn("batch_correlated");
		EventBusEvent<CorrelationDto> event = new EventBusEvent<>();
		event.setPayload(dto);
		event.setId(UUID.randomUUID().toString());
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.batchDetailService).should(times(1)).saveBatchDetail(dto);
		then(this.correlationEventPublisher).should(times(1)).publishCorrelationEvent(dto);
	}

	@Test
	@DisplayName("should call no handler for local bank account event")
	void givenLocalBankAccountCreated_whenConsumeEvent_thenNoHandlerCalled() {
		// Given
		IEvent localBankAccountCreated = mock(IEvent.class);
		given(localBankAccountCreated.type()).willReturn("local_bank_account_created");
		EventBusEvent<IEvent> event = new EventBusEvent<>();
		event.setPayload(localBankAccountCreated);
		event.setId(UUID.randomUUID().toString());
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.organizationEventHandler).shouldHaveNoInteractions();
		then(this.bankAccountEventHandler).shouldHaveNoInteractions();
	}

	@Test
	@DisplayName("should call logoServiceEventHandler for LOGO_UPDATED event")
	void givenLogoUpdated_whenConsumeEvent_thenLogoServiceHandlerCalled() {
		// Given
		IEvent logoUpdatedEvent = mock(IEvent.class);
		given(logoUpdatedEvent.type()).willReturn("logo_updated");
		EventBusEvent<IEvent> event = new EventBusEvent<>();
		event.setPayload(logoUpdatedEvent);
		event.setId(UUID.randomUUID().toString());
		// When
		this.eventConsumer.consumeEvent(event);
		// Then
		then(this.logoServiceEventHandler).should(times(1)).handle(event);
		then(this.organizationEventHandler).shouldHaveNoInteractions();
		then(this.bankAccountEventHandler).shouldHaveNoInteractions();
	}

}
