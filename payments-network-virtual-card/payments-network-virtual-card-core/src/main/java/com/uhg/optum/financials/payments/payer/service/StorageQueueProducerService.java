package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;

public interface StorageQueueProducerService {

	/**
	 * sends a payment request event to the worker queue.
	 * @param dto the payment request data transfer object
	 */
	void sendPaymentEventToQueue(PaymentRequestDto dto);

	/**
	 * sends a virtual card webhook event to the worker queue.
	 * @param dto the Zeta webhook data transfer object
	 */
	void sendVirtualCardEventToQueue(ZetaWebhookDto<?> dto);

	/**
	 * sends a correlation event to the worker queue.
	 * @param dto the correlation data transfer object
	 */
	void sendCorrelationEventToQueue(CorrelationDto dto);

}
