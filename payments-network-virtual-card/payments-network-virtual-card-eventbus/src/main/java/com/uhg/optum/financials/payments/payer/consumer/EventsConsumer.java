package com.uhg.optum.financials.payments.payer.consumer;

import com.uhg.optum.financials.payments.payer.config.EventBusProperties;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.service.CorrelationEventPublisher;
import com.uhg.optum.financials.payments.payer.service.CustomRemarkService;
import com.uhg.optum.financials.payments.payer.service.IEventBusHandler;
import com.uhg.optum.financials.payments.payer.service.IEventHandlerRetryExecutorService;
import com.uhg.optum.financials.payments.remittance.events.CustomRemarkUpdated;
import com.uhg.optum.financials.payments.remittance.events.DefaultCustomRemarks;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.client.events.IEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusConsumer;
import jakarta.validation.constraints.NotNull;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.stereotype.Service;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.function.Consumer;

import static com.uhg.optum.financials.payments.bankaccount.events.BankAccountEventNames.BANK_ACCOUNT_CHANGED;
import static com.uhg.optum.financials.payments.bankaccount.events.BankAccountEventNames.BANK_ACCOUNT_CLOSED;
import static com.uhg.optum.financials.payments.bankaccount.events.BankAccountEventNames.BANK_ACCOUNT_CREATED;
import static com.uhg.optum.financials.payments.bankaccount.events.BankAccountEventNames.BANK_ACCOUNT_UNVERIFIED;
import static com.uhg.optum.financials.payments.bankaccount.events.BankAccountEventNames.BANK_ACCOUNT_VERIFIED;
import static com.uhg.optum.financials.payments.bankaccount.events.LocalBankAccountEventNames.LOCAL_BANK_ACCOUNT_CREATED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.ACCOUNT_REGISTERED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.ACCOUNT_UPDATED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.LOGO_UPDATED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.ORGANIZATION_CLOSED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.ORGANIZATION_REGISTERED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.ORGANIZATION_UPDATED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.SUB_ACCOUNT_REGISTERED;
import static com.uhg.optum.financials.payments.organization.events.OrganizationEventNames.SUB_ACCOUNT_UPDATED;

@Slf4j
@Service
public class EventsConsumer implements EventBusConsumer {

	@Autowired
	@Qualifier("organizationEventHandler")
	private IEventBusHandler organizationEventHandler;

	@Autowired
	@Qualifier("bankAccountEventHandler")
	private IEventBusHandler bankAccountEventHandler;

	@Autowired
	@Qualifier("localBankAccountEventHandler")
	private IEventBusHandler localBankAccountEventHandler;

	@Autowired
	@Qualifier("logoServiceEventHandler")
	private IEventBusHandler logoServiceEventHandler;

	@Autowired
	private BatchDetailService batchDetailService;

	@Autowired
	private CorrelationEventPublisher correlationEventPublisher;

	@Autowired
	private CustomRemarkService customRemarkEventService;

	@Autowired
	private IEventHandlerRetryExecutorService iEventHandlerRetryExecutorService;

	@Autowired
	private EventBusProperties eventBusProperties;

	private final Map<String, Consumer<EventBusEvent<? extends IEvent>>> eventHandlers = new HashMap<>();

	public EventsConsumer() {
		this.eventHandlers.put(AppConstants.BATCH_CORRELATED, this::handleBatchDetailEvent);
		this.eventHandlers.put(AppConstants.CUSTOM_REMARK_EVENT_TYPE, this::handleCustomRemarkEvent);
		this.eventHandlers.put(AppConstants.DEFAULT_CUSTOM_REMARK_EVENT_TYPE, this::handleDefaultCustomRemarksEvent);

		this.eventHandlers.put(ORGANIZATION_REGISTERED, this::handleOrganizationEvents);
		this.eventHandlers.put(ORGANIZATION_UPDATED, this::handleOrganizationEvents);
		this.eventHandlers.put(ORGANIZATION_CLOSED, this::handleOrganizationEvents);

		this.eventHandlers.put(ACCOUNT_REGISTERED, this::handleOrganizationEvents);
		this.eventHandlers.put(ACCOUNT_UPDATED, this::handleOrganizationEvents);

		this.eventHandlers.put(SUB_ACCOUNT_REGISTERED, this::handleOrganizationEvents);
		this.eventHandlers.put(SUB_ACCOUNT_UPDATED, this::handleOrganizationEvents);

		this.eventHandlers.put(BANK_ACCOUNT_CREATED, this::handleBankAccountEvents);
		this.eventHandlers.put(BANK_ACCOUNT_CHANGED, this::handleBankAccountEvents);
		this.eventHandlers.put(BANK_ACCOUNT_UNVERIFIED, this::handleBankAccountEvents);
		this.eventHandlers.put(BANK_ACCOUNT_VERIFIED, this::handleBankAccountEvents);
		this.eventHandlers.put(BANK_ACCOUNT_CLOSED, this::handleBankAccountEvents);

		this.eventHandlers.put(LOGO_UPDATED, this::handleLogoUpdated);
		this.eventHandlers.put(LOCAL_BANK_ACCOUNT_CREATED, this::handleLocalBankAccountEvents);

	}

	@Override
	@KafkaListener(topics = "#{__listener.kafkaTopics()}", errorHandler = "kafkaUnknownEventHandler")
	public <T extends IEvent> void consumeEvent(EventBusEvent<T> event) {
		if (event == null || event.getPayload() == null || StringUtils.isEmpty(event.getPayload().type())) {
			log.warn("Skipping message processing as event or payload type is null");
			return;
		}

		String type = event.getPayload().type();
		log.info("Message received of type {}: and id {}", type, event.getId());

		try {
			this.iEventHandlerRetryExecutorService.callEventHandlersWithRetry(event, type, this.eventHandlers.get(type),
					this::handleUnknownEvent);
		}
		catch (Exception ex) {
			log.error("Error while processing message of type {}: {}", type, ex.getMessage(), ex);
		}
	}

	private void handleOrganizationEvents(EventBusEvent<? extends IEvent> event) {
		this.organizationEventHandler.handle(event);
	}

	private void handleBankAccountEvents(EventBusEvent<? extends IEvent> event) {
		this.bankAccountEventHandler.handle(event);
	}

	private void handleLocalBankAccountEvents(EventBusEvent<? extends IEvent> event) {
		this.localBankAccountEventHandler.handle(event);
	}

	private void handleLogoUpdated(EventBusEvent<? extends IEvent> event) {
		this.logoServiceEventHandler.handle(event);
	}

	private void handleUnknownEvent(@NotNull EventBusEvent<? extends IEvent> event) {
		log.warn("Unknown event type: {} with event id: {}", event.type(), event.getEventId());
	}

	private void handleBatchDetailEvent(EventBusEvent<? extends IEvent> eventBusEvent) {
		CorrelationDto dto = (CorrelationDto) eventBusEvent.getPayload();
		this.batchDetailService.saveBatchDetail(dto);
		this.correlationEventPublisher.publishCorrelationEvent(dto);
	}

	private void handleCustomRemarkEvent(EventBusEvent<? extends IEvent> event) {
		CustomRemarkUpdated eventPayload = (CustomRemarkUpdated) event.getPayload();
		this.customRemarkEventService.saveCustomRemark(eventPayload);
	}

	private void handleDefaultCustomRemarksEvent(EventBusEvent<? extends IEvent> event) {
		DefaultCustomRemarks eventPayload = (DefaultCustomRemarks) event.getPayload();
		this.customRemarkEventService.saveDefaultCustomRemarks(eventPayload);
	}

	public List<String> kafkaTopics() {
		return this.eventBusProperties.topics();
	}

}
