package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.config.LoadPaymentCardCallbackProperties;
import com.uhg.optum.financials.payments.payer.constant.EventTypeConstants;
import com.uhg.optum.financials.payments.payer.dto.CardCreationStatusDto;
import com.uhg.optum.financials.payments.payer.dto.callback.CreateCardData;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.service.LoadPaymentCardCallbackService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardDetailsService;
import com.uhg.optum.financials.payments.payer.service.ZetaWebhookService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.scheduling.TaskScheduler;
import org.springframework.stereotype.Service;

import java.math.BigDecimal;
import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_ACCEPTED;

@Service
@Slf4j
public class LoadPaymentCardCallbackServiceImpl implements LoadPaymentCardCallbackService {

	private static final String SPEC_VERSION = "1.0";

	private final LoadPaymentCardCallbackProperties callbackProperties;

	private final VirtualCardDetailsService virtualCardDetailsService;

	private final CardCreationRepository cardCreationRepository;

	private final ZetaWebhookService zetaWebhookService;

	private final LogUtility logUtility;

	private final TaskScheduler taskScheduler;

	@Autowired
	public LoadPaymentCardCallbackServiceImpl(LoadPaymentCardCallbackProperties callbackProperties,
			VirtualCardDetailsService virtualCardDetailsService, CardCreationRepository cardCreationRepository,
			ZetaWebhookService zetaWebhookService, LogUtility logUtility,
			@Qualifier("loadPaymentCardCallbackTaskScheduler") TaskScheduler taskScheduler) {
		this.callbackProperties = callbackProperties;
		this.virtualCardDetailsService = virtualCardDetailsService;
		this.cardCreationRepository = cardCreationRepository;
		this.zetaWebhookService = zetaWebhookService;
		this.logUtility = logUtility;
		this.taskScheduler = taskScheduler;
	}

	@Override
	public void pollAndCallback(String paymentId) {
		if (!this.callbackProperties.isEnabled()) {
			return;
		}
		if (paymentId == null || paymentId.isBlank()) {
			this.logUtility.logMessage("Skipping async callback because paymentId is blank", ERROR, log);
			return;
		}

		List<Long> retryDelaysMinutes = this.callbackProperties.getRetryDelaysMinutes();
		if (retryDelaysMinutes == null || retryDelaysMinutes.isEmpty()) {
			retryDelaysMinutes = List.of(1L, 2L, 3L, 4L);
		}

		scheduleAttempt(paymentId.trim(), retryDelaysMinutes, 0);
	}

	private void scheduleAttempt(String paymentId, List<Long> retryDelaysMinutes, int attemptIndex) {
		if (attemptIndex >= retryDelaysMinutes.size()) {
			this.logUtility.logMessage("All async card callback retries exhausted for paymentId: " + paymentId, ERROR,
					log);
			return;
		}

		long delayInMinutes = sanitizeDelayMinutes(retryDelaysMinutes.get(attemptIndex));
		Instant scheduledTime = Instant.now().plus(Duration.ofMinutes(delayInMinutes));
		int attemptNumber = attemptIndex + 1;
		this.logUtility.logMessage("Scheduling card callback attempt " + attemptNumber + " for paymentId: " + paymentId
				+ " after " + delayInMinutes + " minute(s)", INFO, log);
		this.taskScheduler.schedule(() -> executeAttempt(paymentId, retryDelaysMinutes, attemptIndex), scheduledTime);
	}

	private void executeAttempt(String paymentId, List<Long> retryDelaysMinutes, int attemptIndex) {
		int attemptNumber = attemptIndex + 1;
		try {
			validateRequestAcceptedStatus(paymentId);
			ZetaWebhookDto<CreateCardData> webhookDto = buildWebhookPayload(paymentId);
			this.zetaWebhookService.processZetaWebhook(webhookDto);
			this.logUtility.logMessage(
					"Card callback submitted successfully for paymentId: " + paymentId + " on attempt " + attemptNumber,
					INFO, log);
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Card callback attempt " + attemptNumber + " failed for paymentId: " + paymentId
					+ " due to: " + ex.getMessage(), ERROR, log);
			scheduleAttempt(paymentId, retryDelaysMinutes, attemptIndex + 1);
		}
	}

	private void validateRequestAcceptedStatus(String paymentId) {
		CardCreation cardCreation = this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId)
			.orElseThrow(() -> new IllegalStateException("Card creation record not found for paymentId: " + paymentId));
		if (!REQUEST_ACCEPTED.name().equalsIgnoreCase(cardCreation.getStatus())) {
			throw new IllegalStateException("Card creation status is not REQUEST_ACCEPTED for paymentId: " + paymentId
					+ ". Current status: " + cardCreation.getStatus());
		}
	}

	private ZetaWebhookDto<CreateCardData> buildWebhookPayload(String paymentId) throws Exception {
		CardCreationStatusDto statusDto = this.virtualCardDetailsService.getRawCardCreationStatus(paymentId);

		CreateCardData createCardData = new CreateCardData();
		createCardData.setClientRequestId(UUID.fromString(statusDto.getClientRequestId()));
		createCardData.setCardGuid(UUID.fromString(statusDto.getData().getCardGuid()));
		createCardData.setSpendRules(statusDto.getData().getSpendRules());
		createCardData.setInitialBalance(mapInitialBalance(statusDto));
		createCardData.setAutoActivateCard(statusDto.getData().getAutoActivateCard());
		createCardData.setSingleSpend(statusDto.getData().getSingleSpend());

		return new ZetaWebhookDto<>(UUID.randomUUID().toString(), SPEC_VERSION, Instant.now(), statusDto.getStatus(),
				EventTypeConstants.VIRTUAL_CARD_CREATE, createCardData, null);
	}

	private CreateCardData.InitialBalance mapInitialBalance(CardCreationStatusDto statusDto) {
		CreateCardData.InitialBalance initialBalance = new CreateCardData.InitialBalance();
		if (statusDto.getData().getInitialBalance() == null) {
			return initialBalance;
		}
		initialBalance.setCurrency(statusDto.getData().getInitialBalance().getCurrency());
		BigDecimal amount = statusDto.getData().getInitialBalance().getAmount();
		if (amount != null) {
			initialBalance.setAmount(amount.longValue());
		}
		return initialBalance;
	}

	private long sanitizeDelayMinutes(Long minutes) {
		return (minutes == null || minutes < 0) ? 0 : minutes;
	}

}
