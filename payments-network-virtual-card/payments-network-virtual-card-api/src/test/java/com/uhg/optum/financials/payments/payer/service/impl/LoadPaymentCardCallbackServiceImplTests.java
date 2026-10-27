package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.config.LoadPaymentCardCallbackProperties;
import com.uhg.optum.financials.payments.payer.constant.EventTypeConstants;
import com.uhg.optum.financials.payments.payer.dto.CardCreationStatusDto;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.service.VirtualCardDetailsService;
import com.uhg.optum.financials.payments.payer.service.ZetaWebhookService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.scheduling.TaskScheduler;

import java.time.Instant;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_ACCEPTED;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_INITIATED;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.times;

@ExtendWith(MockitoExtension.class)
class LoadPaymentCardCallbackServiceImplTests {

	@Mock
	private LoadPaymentCardCallbackProperties callbackProperties;

	@Mock
	private VirtualCardDetailsService virtualCardDetailsService;

	@Mock
	private CardCreationRepository cardCreationRepository;

	@Mock
	private ZetaWebhookService zetaWebhookService;

	@Mock
	private LogUtility logUtility;

	@Mock
	private TaskScheduler taskScheduler;

	@InjectMocks
	private LoadPaymentCardCallbackServiceImpl service;

	@Test
	void pollAndCallback_shouldReturnImmediatelyWhenDisabled() throws Exception {
		given(this.callbackProperties.isEnabled()).willReturn(false);

		this.service.pollAndCallback("PAYMENT-1");

		then(this.taskScheduler).should(never()).schedule(any(Runnable.class), any(Instant.class));
		then(this.virtualCardDetailsService).should(never()).getRawCardCreationStatus(any());
		then(this.zetaWebhookService).should(never()).processZetaWebhook(any());
	}

	@Test
	void pollAndCallback_shouldBuildAndSubmitWebhookOnSuccess() throws Exception {
		String paymentId = "PAYMENT-123";
		UUID requestId = UUID.fromString("3122d323-0c58-4929-8548-58d72cd9a43b");
		UUID cardGuid = UUID.fromString("7038392d-5fc8-44ea-bc1a-ad2c71cdb98c");
		given(this.callbackProperties.isEnabled()).willReturn(true);
		given(this.callbackProperties.getRetryDelaysMinutes()).willReturn(List.of(0L, 0L, 0L, 0L));
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.willReturn(Optional.of(buildCardCreation(paymentId, REQUEST_ACCEPTED.name())));

		given(this.virtualCardDetailsService.getRawCardCreationStatus(paymentId))
			.willReturn(buildStatusDto(requestId.toString(), cardGuid.toString(), true));

		this.service.pollAndCallback(paymentId);

		ArgumentCaptor<Runnable> scheduledTaskCaptor = ArgumentCaptor.forClass(Runnable.class);
		then(this.taskScheduler).should(times(1)).schedule(scheduledTaskCaptor.capture(), any(Instant.class));
		then(this.zetaWebhookService).should(never()).processZetaWebhook(any());

		scheduledTaskCaptor.getValue().run();

		ArgumentCaptor<ZetaWebhookDto> webhookCaptor = ArgumentCaptor.forClass(ZetaWebhookDto.class);
		then(this.zetaWebhookService).should(times(1)).processZetaWebhook(webhookCaptor.capture());
		ZetaWebhookDto<?> dto = webhookCaptor.getValue();
		assertThat(dto.status()).isEqualTo("SUCCESS");
		assertThat(dto.type()).isEqualTo(EventTypeConstants.VIRTUAL_CARD_CREATE);
		assertThat(dto.specversion()).isEqualTo("1.0");
		assertThat(dto.data()).isInstanceOf(CreateCardData.class);
		CreateCardData data = (CreateCardData) dto.data();
		assertThat(data.getClientRequestId()).isEqualTo(requestId);
		assertThat(data.getCardGuid()).isEqualTo(cardGuid);
		assertThat(data.getInitialBalance().getAmount()).isEqualTo(50098L);
		assertThat(data.getInitialBalance().getCurrency()).isEqualTo("USD");
		assertThat(data.getSpendRules()).containsExactly("RS1-001", "RS-0002");
	}

	@Test
	void pollAndCallback_shouldRetryUntilSuccess() throws Exception {
		String paymentId = "PAYMENT-123";
		UUID requestId = UUID.fromString("3122d323-0c58-4929-8548-58d72cd9a43b");
		given(this.callbackProperties.isEnabled()).willReturn(true);
		given(this.callbackProperties.getRetryDelaysMinutes()).willReturn(List.of(1L, 2L, 3L, 4L));
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.willReturn(Optional.of(buildCardCreation(paymentId, REQUEST_ACCEPTED.name())));

		given(this.virtualCardDetailsService.getRawCardCreationStatus(paymentId))
			.willThrow(new RuntimeException("temporary"))
			.willReturn(buildStatusDto(requestId.toString(), "7038392d-5fc8-44ea-bc1a-ad2c71cdb98c", false));

		this.service.pollAndCallback(paymentId);

		ArgumentCaptor<Runnable> scheduledTaskCaptor = ArgumentCaptor.forClass(Runnable.class);
		ArgumentCaptor<Instant> scheduledTimeCaptor = ArgumentCaptor.forClass(Instant.class);
		then(this.taskScheduler).should(times(1))
			.schedule(scheduledTaskCaptor.capture(), scheduledTimeCaptor.capture());
		scheduledTaskCaptor.getValue().run();
		then(this.taskScheduler).should(times(2))
			.schedule(scheduledTaskCaptor.capture(), scheduledTimeCaptor.capture());
		List<Instant> scheduledTimes = scheduledTimeCaptor.getAllValues();
		assertThat(scheduledTimes.get(scheduledTimes.size() - 1)).isAfter(scheduledTimes.getFirst());
		List<Runnable> scheduledTasks = scheduledTaskCaptor.getAllValues();
		scheduledTasks.get(scheduledTasks.size() - 1).run();

		then(this.virtualCardDetailsService).should(times(2)).getRawCardCreationStatus(paymentId);
		then(this.zetaWebhookService).should(times(1)).processZetaWebhook(any());
	}

	@Test
	void pollAndCallback_shouldRetryUntilCardCreationStatusIsRequestAccepted() throws Exception {
		String paymentId = "PAYMENT-123";
		UUID requestId = UUID.fromString("3122d323-0c58-4929-8548-58d72cd9a43b");
		given(this.callbackProperties.isEnabled()).willReturn(true);
		given(this.callbackProperties.getRetryDelaysMinutes()).willReturn(List.of(0L, 0L, 0L, 0L));
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId)).willReturn(
				Optional.of(buildCardCreation(paymentId, REQUEST_INITIATED.name())),
				Optional.of(buildCardCreation(paymentId, REQUEST_ACCEPTED.name())));
		given(this.virtualCardDetailsService.getRawCardCreationStatus(paymentId))
			.willReturn(buildStatusDto(requestId.toString(), "7038392d-5fc8-44ea-bc1a-ad2c71cdb98c", true));

		this.service.pollAndCallback(paymentId);

		ArgumentCaptor<Runnable> scheduledTaskCaptor = ArgumentCaptor.forClass(Runnable.class);
		then(this.taskScheduler).should(times(1)).schedule(scheduledTaskCaptor.capture(), any(Instant.class));
		scheduledTaskCaptor.getValue().run();
		then(this.virtualCardDetailsService).should(never()).getRawCardCreationStatus(paymentId);
		then(this.taskScheduler).should(times(2)).schedule(scheduledTaskCaptor.capture(), any(Instant.class));

		List<Runnable> scheduledTasks = scheduledTaskCaptor.getAllValues();
		scheduledTasks.get(scheduledTasks.size() - 1).run();

		then(this.cardCreationRepository).should(times(2)).findTopByPaymentIdOrderByCreatedAtDesc(paymentId);
		then(this.virtualCardDetailsService).should(times(1)).getRawCardCreationStatus(paymentId);
		then(this.zetaWebhookService).should(times(1)).processZetaWebhook(any());
	}

	private CardCreationStatusDto buildStatusDto(String requestId, String cardGuid, boolean withBalance) {
		CardCreationStatusDto dto = new CardCreationStatusDto();
		dto.setClientRequestId(requestId);
		dto.setStatus("SUCCESS");

		CardCreationStatusDto.CardData data = new CardCreationStatusDto.CardData();
		data.setCardGuid(cardGuid);
		data.setSpendRules(List.of("RS1-001", "RS-0002"));
		if (withBalance) {
			CardCreationStatusDto.InitialBalance balance = new CardCreationStatusDto.InitialBalance();
			balance.setAmount(new java.math.BigDecimal("50098"));
			balance.setCurrency("USD");
			data.setInitialBalance(balance);
		}
		dto.setData(data);
		return dto;
	}

	private CardCreation buildCardCreation(String paymentId, String status) {
		CardCreation cardCreation = new CardCreation();
		cardCreation.setPaymentId(paymentId);
		cardCreation.setStatus(status);
		return cardCreation;
	}

}
