package com.uhg.optum.financials.payments.payer.producer;

import com.uhg.optum.financials.shared.core.beans.HealthDetail;
import com.uhg.optum.financials.shared.worker.service.IWorkerPublisher;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.context.annotation.DependsOn;
import org.springframework.stereotype.Service;

import java.io.Serializable;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.FAILED;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.SUCCESS;

@Service
@Slf4j
@DependsOn("workerProducerConfig")
public class StorageQueueProducerApi {

	private final IWorkerPublisher publisher;

	public StorageQueueProducerApi(@Qualifier("defaultWorkerProducer") IWorkerPublisher publisher) {
		this.publisher = publisher;
	}

	public <T extends Serializable> String sendMessageToQueue(T t) {
		try {
			this.publisher.publishEvent(t);

			return SUCCESS;
		}
		catch (Exception ex) {
			log.error("Exception in sendMessageBinaryDataAsync :{}", ex);
			return FAILED;
		}
	}

	public HealthDetail checkHealthPublisher() {
		log.info("Health check for producer");
		return this.publisher.getHealth();
	}

}
