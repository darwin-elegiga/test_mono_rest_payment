package com.uhg.optum.financials.payments.features.cardCreation.implementation;

import com.fasterxml.jackson.databind.node.ObjectNode;
import com.uhg.optum.financials.payments.APITrigger.APITriggers;
import com.uhg.optum.financials.payments.common.CommonImpl;
import com.uhg.optum.financials.payments.payer.dto.FileTrackingDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentResponseDto;
import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import org.assertj.core.api.Assertions;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.util.UUID;
import java.util.concurrent.TimeUnit;

import static org.awaitility.Awaitility.await;

public abstract class CardCreationImpl extends CommonImpl {

	private static final Logger log = LoggerFactory.getLogger(CardCreationImpl.class);

	private final APITriggers apiTriggers = APITriggers.getAPITriggersInstance();

	private final int waitToLoadEntity = 6;

	protected void testCardCreation() throws IOException {
		PaymentRequestDto paymentRequestDto = prepareCardFlow(getOrgShortCode(), getAccountShortCode(),
				getSubAccountShortCode());
		publishBatchEventAndValidatePaymentCompleted(paymentRequestDto);
		validatePaymentFilesCreated(paymentRequestDto.getCorrelationId());
	}

	protected void publishBatchEventAndValidatePaymentCompleted(PaymentRequestDto paymentRequestDto)
			throws IOException {
		String paymentId = paymentRequestDto.getPaymentDetails().getId();
		String correlationId = paymentRequestDto.getCorrelationId();

		ObjectNode requestBody = this.apiTriggers.objectMapper.createObjectNode();
		requestBody.put("id", UUID.randomUUID().toString());
		requestBody.put("tenantId", UUID.randomUUID().toString());
		requestBody.put("eventId", UUID.randomUUID().toString());

		ObjectNode correlationDto = requestBody.putObject("correlationDto");
		correlationDto.put("org", paymentRequestDto.getOrgShortCode());
		correlationDto.put("correlationId", correlationId);
		correlationDto.put("numberOfBatches", 1);
		correlationDto.put("type", "batch_correlated");

		ObjectNode actor = correlationDto.putObject("actor");
		actor.put("requestedBy", "string");
		actor.put("userId", "string");

		ObjectNode batch = correlationDto.putArray("batches").addObject();
		batch.put("batchId", generateRandomAlphaNumericString(15));

		ObjectNode network = batch.putArray("networks").addObject();
		network.put("network", "VIRTUALCARD");
		network.put("numberOfPayments", 1);
		network.put("totalAmount", paymentRequestDto.getPaymentDetails().getAmount());

		retrofit2.Response<okhttp3.ResponseBody> response = this.apiTriggers.publishBatchEvent(requestBody);
		Assertions.assertThat(response.code()).isEqualTo(200);

		this.apiEndPointsImpl.isBatchEventRegister(correlationId);

		await().atMost(this.waitToLoadEntity, TimeUnit.MINUTES).pollInterval(10, TimeUnit.SECONDS).untilAsserted(() -> {
			PaymentResponseDto updatedPayment = this.apiEndPointsImpl.getPaymentDetails(paymentId);
			Assertions.assertThat(updatedPayment.getPaymentStatus()).isEqualTo("PC");
			log.info("Payment status moved to PC for correlationId {}", correlationId);
		});
	}

	protected void validatePaymentFilesCreated(String correlationId) throws IOException {
		await().atMost(7, TimeUnit.MINUTES).pollInterval(10, TimeUnit.SECONDS).untilAsserted(() -> {
			FileTrackingDto paymentFiles = this.apiEndPointsImpl.getPaymentFiles(correlationId);
			Assertions.assertThat(paymentFiles.get_835()).as("835 file should be available").isNotBlank();
			Assertions.assertThat(paymentFiles.getCompanion()).as("companion file should be available").isNotBlank();
			log.info("Payment files created for correlationId {}", correlationId);
		});
	}

}
