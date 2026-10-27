package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.organization.events.AccountUpdated;
import com.uhg.optum.financials.payments.payer.dto.AccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.AccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.Account;
import com.uhg.optum.financials.payments.payer.repository.AccountRepository;
import com.uhg.optum.financials.payments.payer.service.AccountPublisherService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.DEBUG;
import static com.uhg.optum.financials.payments.payer.constant.Constants.ACCOUNT_REGISTERED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.ACCOUNT_UPDATED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.FAILED;
import static com.uhg.optum.financials.payments.payer.constant.Constants.SUCCESS;

@Service
@Slf4j
public class AccountPublisherServiceImpl implements AccountPublisherService {

	private final EventBusProducer producerService;

	private final AccountRepository accountRepository;

	private final LogUtility logUtility;

	public AccountPublisherServiceImpl(EventBusProducer producerService, AccountRepository accountRepository,
			LogUtility logUtility) {
		this.producerService = producerService;
		this.accountRepository = accountRepository;
		this.logUtility = logUtility;
	}

	@Override
	public String publishAccountRegisteredEvent(AccountRegisteredEvent accountRegisteredEvent) {
		try {
			EventBusEvent<AccountRegistered> event = convertAccountDtoToEventMessage(accountRegisteredEvent);
			log.info("Sending message to event hub {}", event.getType());
			this.producerService.sendEvent(event, null);
			return SUCCESS;
		}
		catch (Exception ex) {
			log.error("Exception occurred while sending message to event bus :{}", ex.getMessage());
			return FAILED;
		}
	}

	@Override
	public String publishAccountUpdatedEvent(AccountUpdatedDto accountUpdatedDto) {
		try {
			EventBusEvent<AccountUpdated> event = convertAccountUpdateDtoToEventMessage(accountUpdatedDto);
			log.info("Sending account updated message to event hub {}", event.getType());
			this.producerService.sendEvent(event, null);
			return SUCCESS;
		}
		catch (Exception ex) {
			log.error("Exception occurred while sending account updated message to event bus :{}", ex.getMessage());
			return FAILED;
		}
	}

	@Override
	public Account getAccountRegDetails(String account) {
		try {
			UUID accountId = UUID.fromString(account);
			Account accountRegistrationDetails = this.accountRepository.findByAccountId(accountId);
			if (accountRegistrationDetails != null) {
				return accountRegistrationDetails;
			}
			else {
				this.logUtility.logMessage("Account reg details not found " + accountId, DEBUG, log);
				throw new NotFoundException("Account registration details not found " + accountId);
			}
		}
		catch (Exception ex) {
			log.error("Error while fetching account details", ex);
			throw new InternalServerException("Database error occurred while fetching account details");
		}
	}

	private EventBusEvent<AccountRegistered> convertAccountDtoToEventMessage(
			AccountRegisteredEvent accountRegisteredEvent) {
		EventBusEvent<AccountRegistered> eventMessage = new EventBusEvent<>();
		eventMessage.setId(String.valueOf(accountRegisteredEvent.getId()));
		eventMessage.setTenantId(String.valueOf(accountRegisteredEvent.getTenantId()));
		eventMessage.setEventId(accountRegisteredEvent.getEventId());
		eventMessage.setPayload(accountRegisteredEvent.getAccountRegistered());
		eventMessage.setType(ACCOUNT_REGISTERED);
		eventMessage.setEntityName("virtual-card");
		eventMessage.setSource("Virtual-Card-MOCK");
		eventMessage.setCreatedOn(Instant.now());
		return eventMessage;
	}

	private EventBusEvent<AccountUpdated> convertAccountUpdateDtoToEventMessage(AccountUpdatedDto accountUpdatedDto) {
		EventBusEvent<AccountUpdated> eventMessage = new EventBusEvent<>();
		eventMessage.setId(String.valueOf(accountUpdatedDto.getId()));
		eventMessage.setTenantId(String.valueOf(accountUpdatedDto.getTenantId()));
		eventMessage.setEventId(accountUpdatedDto.getEventId());
		eventMessage.setPayload(accountUpdatedDto.getAccountupdated());
		eventMessage.setType(ACCOUNT_UPDATED);
		eventMessage.setEntityName("virtual-card");
		eventMessage.setSource("Virtual-Card-MOCK");
		eventMessage.setCreatedOn(Instant.now());
		return eventMessage;
	}

}
