package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.service.StorageQueueProducer;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import com.uhg.optum.financials.shared.worker.service.IWorkerPublisher;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.context.annotation.DependsOn;
import org.springframework.stereotype.Service;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.CREATE_CARD_MSG_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.PAYMENT_MSG_EXCEPTION_LOG;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;

@Service
@DependsOn("workerProducerConfig")
@Slf4j
public class StorageQueueProducerImpl implements StorageQueueProducer {

	private final LogUtility logUtility;

	private final IWorkerPublisher publisher;

	public StorageQueueProducerImpl(LogUtility logUtility,
			@Qualifier("defaultWorkerProducer") IWorkerPublisher publisher) {
		this.logUtility = logUtility;
		this.publisher = publisher;
	}

	@Override
	public void sendPaymentEventToQueue(PaymentRequestDto dto) {
		try {
			this.publisher.publishEvent(dto);
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority(
					PAYMENT_MSG_EXCEPTION_LOG + ex.getMessage() + " Payment Id: " + dto.getPaymentDetails().getId(),
					ERROR, log, P3);
		}
	}

	@Override
	public void sendVirtualCardEventToQueue(ZetaWebhookDto dto) {
		try {
			this.publisher.publishEvent(dto);
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority(
					CREATE_CARD_MSG_EXCEPTION_LOG + ex.getMessage() + " Correlation Id: " + dto, ERROR, log, P3);
		}
	}

	@Override
	public void sendCorrelationEventToQueue(CorrelationDto dto) {
		try {
			this.publisher.publishEvent(dto);
		}
		catch (Exception ex) {
			this.logUtility.logMessageWithPriority(
					"Correlation event publish exception: " + ex.getMessage() + " Correlation Id: " + dto, ERROR, log,
					P3);
		}
	}

}
