package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;

public interface StorageQueueProducer {

	void sendPaymentEventToQueue(PaymentRequestDto dto);

	void sendVirtualCardEventToQueue(ZetaWebhookDto dto);

	void sendCorrelationEventToQueue(CorrelationDto dto);

}
