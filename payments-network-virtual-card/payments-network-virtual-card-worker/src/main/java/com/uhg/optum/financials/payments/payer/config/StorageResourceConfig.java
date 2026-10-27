package com.uhg.optum.financials.payments.payer.config;

import com.azure.storage.blob.BlobClientBuilder;
import com.uhg.optum.financials.payments.payer.dto.SetECCKeysDto;
import com.uhg.optum.financials.shared.worker.service.IWorkerPublisher;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.DependsOn;
import org.springframework.context.event.EventListener;

@DependsOn("workerProducerConfig")
@Configuration
@Slf4j
public class StorageResourceConfig {

	private final IWorkerPublisher publisher;

	public StorageResourceConfig(@Qualifier("defaultWorkerProducer") IWorkerPublisher publisher) {
		this.publisher = publisher;
	}

	@Bean
	public BlobClientBuilder blobClientBuilder() {
		return new BlobClientBuilder();
	}

	@EventListener(ApplicationReadyEvent.class)
	public void createResources() {
		this.createAsymmetricKeys();
	}

	private void createAsymmetricKeys() {
		SetECCKeysDto setECCKeysDto = new SetECCKeysDto();
		setECCKeysDto.setSetECCKeys(true);
		this.publisher.publishEvent(setECCKeysDto);
	}

}
