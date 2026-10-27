package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.model.CardCancellation;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.CardReset;
import com.uhg.optum.financials.payments.payer.repository.CardCancellationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardResetRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.util.Optional;
import java.util.UUID;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.Constants.CANCEL;
import static com.uhg.optum.financials.payments.payer.constant.Constants.CREATE;
import static com.uhg.optum.financials.payments.payer.constant.Constants.RESET;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.REQUEST_ACCEPTED;

@Component
@Slf4j
public class WebhookAlertUtil {

	@Value("${webhook.cutoff-time}")
	private int cutoffTime;

	private final ScheduledExecutorService scheduler;

	private final LogUtility logUtility;

	private final CardCreationRepository cardCreationRepository;

	private final CardCancellationRepository cardCancellationRepository;

	private final CardResetRepository cardResetRepository;

	public WebhookAlertUtil(ScheduledExecutorService scheduler, LogUtility logUtility,
			CardCreationRepository cardCreationRepository, CardCancellationRepository cardCancellationRepository,
			CardResetRepository cardResetRepository) {
		this.scheduler = scheduler;
		this.logUtility = logUtility;
		this.cardCreationRepository = cardCreationRepository;
		this.cardCancellationRepository = cardCancellationRepository;
		this.cardResetRepository = cardResetRepository;
	}

	public void scheduleTimeout(UUID id, String type) {
		this.scheduler.schedule(() -> {
			try {
				switch (type) {
					case CREATE:
						createAlert(id);
						break;

					case CANCEL:
						cancelAlert(id);
						break;

					case RESET:
						resetAlert(id);
						break;
					default:
						log.warn("Invalid operation type for webhook alert scheduling: {}", type);
				}
			}
			catch (Exception ex) {
				log.error("Error occurred while processing scheduled timeout for " + type + " : {}", id, ex);
			}

		}, this.cutoffTime, TimeUnit.SECONDS);

	}

	private void createAlert(UUID cardCreationId) {

		Optional<CardCreation> optionalCardCreation = this.cardCreationRepository.findById(cardCreationId);

		if (optionalCardCreation.isPresent()) {
			CardCreation cardCreation = optionalCardCreation.get();

			if (REQUEST_ACCEPTED.name().equalsIgnoreCase(cardCreation.getStatus())) {
				this.logUtility.logMessageWithPriority("Create webhook response not received within cut-off time.",
						ERROR, log, P3);
			}
		}

	}

	private void cancelAlert(UUID cancelId) {

		Optional<CardCancellation> optionalCardCancellation = this.cardCancellationRepository.findById(cancelId);

		if (optionalCardCancellation.isPresent()) {
			CardCancellation cardCancellation = optionalCardCancellation.get();

			if (REQUEST_ACCEPTED.name().equalsIgnoreCase(cardCancellation.getStatus())) {
				this.logUtility.logMessageWithPriority("Cancel webhook response not received within cut-off time.",
						ERROR, log, P3);
			}
		}

	}

	private void resetAlert(UUID resetId) {

		Optional<CardReset> optionalCardReset = this.cardResetRepository.findById(resetId);

		if (optionalCardReset.isPresent()) {
			CardReset cardReset = optionalCardReset.get();

			if (REQUEST_ACCEPTED.name().equalsIgnoreCase(cardReset.getStatus())) {
				this.logUtility.logMessageWithPriority("Reset webhook response not received within cut-off time.",
						ERROR, log, P3);
			}
		}

	}

}
