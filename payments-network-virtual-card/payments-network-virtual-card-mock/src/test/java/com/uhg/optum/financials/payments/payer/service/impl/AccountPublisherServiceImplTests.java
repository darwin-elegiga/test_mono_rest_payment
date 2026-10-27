package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.organization.events.AccountRegistered;
import com.uhg.optum.financials.payments.organization.events.AccountUpdated;
import com.uhg.optum.financials.payments.payer.dto.AccountRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.AccountUpdatedDto;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.model.Account;
import com.uhg.optum.financials.payments.payer.repository.AccountRepository;
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
class AccountPublisherServiceImplTests {

	@Mock
	private EventBusProducer producerService;

	@Mock
	private AccountRepository accountRepository;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private AccountPublisherServiceImpl accountPublisherServiceImpl;

	private AccountRegisteredEvent accountRegisteredEvent;

	private AccountUpdatedDto accountUpdatedDto;

	private Account account;

	@BeforeEach
	void setUp() {
		this.accountRegisteredEvent = new AccountRegisteredEvent();
		this.accountRegisteredEvent.setId(UUID.randomUUID());
		this.accountRegisteredEvent.setTenantId(UUID.randomUUID());
		this.accountRegisteredEvent.setEventId(UUID.randomUUID());
		this.accountRegisteredEvent.setAccountRegistered(new AccountRegistered());

		this.accountUpdatedDto = new AccountUpdatedDto();
		this.accountUpdatedDto.setId(UUID.randomUUID());
		this.accountUpdatedDto.setTenantId(UUID.randomUUID());
		this.accountUpdatedDto.setEventId(UUID.randomUUID());
		this.accountUpdatedDto.setAccountupdated(new AccountUpdated());

		this.account = new Account();
	}

	@Test
	void publishAccountRegisteredEvent_success() {
		String result = this.accountPublisherServiceImpl.publishAccountRegisteredEvent(this.accountRegisteredEvent);
		then(result).isEqualTo("success");
		org.mockito.BDDMockito.then(this.producerService).should().sendEvent(any(EventBusEvent.class), any());
	}

	@Test
	void publishAccountRegisteredEvent_failure() {
		willThrow(new RuntimeException("fail")).given(this.producerService).sendEvent(any(EventBusEvent.class), any());
		String result = this.accountPublisherServiceImpl.publishAccountRegisteredEvent(this.accountRegisteredEvent);
		then(result).isEqualTo("Failed");
	}

	@Test
	void publishAccountUpdatedEvent_success() {
		String result = this.accountPublisherServiceImpl.publishAccountUpdatedEvent(this.accountUpdatedDto);
		then(result).isEqualTo("success");
		org.mockito.BDDMockito.then(this.producerService).should().sendEvent(any(EventBusEvent.class), any());
	}

	@Test
	void publishAccountUpdatedEvent_failure() {
		willThrow(new RuntimeException("fail")).given(this.producerService).sendEvent(any(EventBusEvent.class), any());
		String result = this.accountPublisherServiceImpl.publishAccountUpdatedEvent(this.accountUpdatedDto);
		then(result).isEqualTo("Failed");
	}

	@Test
	void getAccountRegDetails_found() {
		UUID id = UUID.randomUUID();
		given(this.accountRepository.findByAccountId(id)).willReturn(this.account);
		Account result = this.accountPublisherServiceImpl.getAccountRegDetails(id.toString());
		then(result).isEqualTo(this.account);
	}

	@Test
	void getAccountRegDetails_notFound() {
		UUID id = UUID.randomUUID();
		given(this.accountRepository.findByAccountId(id)).willReturn(null);
		assertThatThrownBy(() -> this.accountPublisherServiceImpl.getAccountRegDetails(id.toString()))
			.isInstanceOf(InternalServerException.class)
			.hasMessageContaining("Database error occurred while fetching account details");
		org.mockito.BDDMockito.then(this.logUtility)
			.should()
			.logMessage(org.mockito.ArgumentMatchers.contains("Account reg details not found"), any(), any());
	}

	@Test
	void convertAccountDtoToEventMessage_shouldMapAllFields() {
		EventBusEvent<AccountRegistered> event = org.springframework.test.util.ReflectionTestUtils.invokeMethod(
				this.accountPublisherServiceImpl, "convertAccountDtoToEventMessage", this.accountRegisteredEvent);
		then(event.getId()).isEqualTo(this.accountRegisteredEvent.getId().toString());
		then(event.getTenantId()).isEqualTo(this.accountRegisteredEvent.getTenantId().toString());
		then(event.getEventId()).isEqualTo(this.accountRegisteredEvent.getEventId());
		org.assertj.core.api.BDDAssertions.then(event.getPayload())
			.isEqualTo(this.accountRegisteredEvent.getAccountRegistered());
		then(event.getType()).isEqualTo("account-registered");
		then(event.getEntityName()).isEqualTo("virtual-card");
		then(event.getSource()).isEqualTo("Virtual-Card-MOCK");
		then(event.getCreatedOn()).isNotNull();
	}

	@Test
	void convertAccountUpdateDtoToEventMessage_shouldMapAllFields() {
		EventBusEvent<AccountUpdated> event = org.springframework.test.util.ReflectionTestUtils.invokeMethod(
				this.accountPublisherServiceImpl, "convertAccountUpdateDtoToEventMessage", this.accountUpdatedDto);
		then(event.getId()).isEqualTo(this.accountUpdatedDto.getId().toString());
		then(event.getTenantId()).isEqualTo(this.accountUpdatedDto.getTenantId().toString());
		then(event.getEventId()).isEqualTo(this.accountUpdatedDto.getEventId());
		org.assertj.core.api.BDDAssertions.then(event.getPayload())
			.isEqualTo(this.accountUpdatedDto.getAccountupdated());
		then(event.getType()).isEqualTo("account-updated");
		then(event.getEntityName()).isEqualTo("virtual-card");
		then(event.getSource()).isEqualTo("Virtual-Card-MOCK");
		then(event.getCreatedOn()).isNotNull();
	}

}
