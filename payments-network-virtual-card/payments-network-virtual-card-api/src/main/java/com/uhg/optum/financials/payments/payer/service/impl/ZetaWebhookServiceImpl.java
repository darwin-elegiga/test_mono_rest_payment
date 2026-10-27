package com.uhg.optum.financials.payments.payer.service.impl;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.constant.ApiConstants;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.exception.QueuePublishException;
import com.uhg.optum.financials.payments.payer.exception.ServiceUnavailableException;
import com.uhg.optum.financials.payments.payer.service.StorageQueueProducer;
import com.uhg.optum.financials.payments.payer.service.ZetaWebhookService;
import com.uhg.optum.financials.payments.payer.util.DuplicateWebhookCheckUtil;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;

@Slf4j
@Service
public class ZetaWebhookServiceImpl implements ZetaWebhookService {

	private final StorageQueueProducer storageQueueProducer;

	private final LogUtility logUtility;

	private final ObjectMapper objectMapper;

	private final DuplicateWebhookCheckUtil duplicateWebhookCheckUtil;

	public ZetaWebhookServiceImpl(StorageQueueProducer storageQueueProducer, LogUtility logUtility,
			ObjectMapper objectMapper, DuplicateWebhookCheckUtil duplicateWebhookCheckUtil) {
		this.storageQueueProducer = storageQueueProducer;
		this.logUtility = logUtility;
		this.objectMapper = objectMapper;
		this.duplicateWebhookCheckUtil = duplicateWebhookCheckUtil;
	}

	@Override
	public int processZetaWebhook(ZetaWebhookDto zetaWebhookDto) {
		try {
			int inserted = this.duplicateWebhookCheckUtil.checkForProcessedWebhook(zetaWebhookDto.id(),
					zetaWebhookDto.type(), ApiConstants.RECEIVED);

			switch (inserted) {
				case 0 -> {
					this.logUtility.logMessage("Duplicate webhook received with ID: %s".formatted(zetaWebhookDto.id()),
							INFO, log);
					return 0;
				}
				case 1 -> {
					String json = this.objectMapper.writeValueAsString(zetaWebhookDto);
					this.logUtility.logMessage("Received ZetaWebhookDto: %s".formatted(json), INFO, log);
					this.storageQueueProducer.sendVirtualCardEventToQueue(zetaWebhookDto);
					this.logUtility.logMessage(
							"Row inserted in Zeta webhook received with ID: %s".formatted(zetaWebhookDto.id()), INFO,
							log);
					return 1;
				}
				default -> {
					this.logUtility.logMessage(
							"Unexpected insert result [%d] for webhook ID: %s".formatted(inserted, zetaWebhookDto.id()),
							ERROR, log);
					throw new IllegalStateException("Unexpected insert result [%d] for webhook ID: %s"
						.formatted(inserted, zetaWebhookDto.id()));
				}
			}

		}
		catch (QueuePublishException ex) {
			this.logUtility.logMessageWithPriority("Error sending Zeta Webhook Response to queue: " + ex.getMessage(),
					ERROR, log, P3);
			throw new ServiceUnavailableException("Error sending Zeta Webhook response to queue: " + ex.getMessage(),
					ex);
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Error sending Zeta Webhook Response to queue: " + ex.getMessage(), ERROR, log);
			throw new IllegalArgumentException("Error sending Zeta Webhook response to queue: " + ex.getMessage());
		}
	}

}
