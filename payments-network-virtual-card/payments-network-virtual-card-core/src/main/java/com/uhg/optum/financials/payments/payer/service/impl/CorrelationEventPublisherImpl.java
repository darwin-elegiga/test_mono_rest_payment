package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.constant.ValidationConstants;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.service.CorrelationEventPublisher;
import com.uhg.optum.financials.payments.payer.service.StorageQueueProducer;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CORRELATION_EVENT_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CORRELATION_MSG_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;

@Slf4j
@Service
public class CorrelationEventPublisherImpl implements CorrelationEventPublisher {

	private final StorageQueueProducer storageQueueProducer;

	private final LogUtility logUtility;

	public CorrelationEventPublisherImpl(StorageQueueProducer storageQueueProducer, LogUtility logUtility) {
		this.storageQueueProducer = storageQueueProducer;
		this.logUtility = logUtility;
	}

	/**
	 * publishes a correlation event to the worker queue for processing.
	 *
	 * this method takes a CorrelationDto containing batch and virtual card information
	 * and publishes it to the worker queue for asynchronous processing.
	 * @param correlationDto the correlation data containing batch and virtual card
	 * information
	 * @throws BadRequestException if publishing to the worker queue fails
	 */
	@Override
	public void publishCorrelationEvent(CorrelationDto correlationDto) {
		log.debug("Publishing correlation event to worker queue for correlation ID: {}",
				correlationDto.getCorrelationId());

		try {
			this.storageQueueProducer.sendCorrelationEventToQueue(correlationDto);
			log.info("Correlation event published successfully for correlation ID: {}",
					correlationDto.getCorrelationId());
		}
		catch (Exception ex) {
			log.error("Exception occurred while publishing correlation event to worker queue: {}", ex.getMessage(), ex);
			this.logUtility.logMessageWithPriority(CORRELATION_MSG_EXCEPTION_LOG + ex.getMessage() + " Correlation Id:"
					+ correlationDto.getCorrelationId(), ValidationConstants.ERROR, log, P3);
			throw new BadRequestException(CORRELATION_EVENT_EXCEPTION_LOG + ex.getMessage());
		}
	}

}
