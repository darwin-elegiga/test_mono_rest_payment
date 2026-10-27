package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.payments.payer.dto.SetECCKeysDto;
import com.uhg.optum.financials.shared.worker.service.IWorkerPublisher;
import org.assertj.core.api.BDDAssertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import org.mockito.Mockito;

@ExtendWith(MockitoExtension.class)
class StorageResourceConfigTests {

	@InjectMocks
	private StorageResourceConfig storageResourceConfig;

	@Mock
	private IWorkerPublisher publisher;

	@Test
	void testBlobClientBuilder() {
		var blobClientBuilder = this.storageResourceConfig.blobClientBuilder();

		BDDAssertions.then(blobClientBuilder).isNotNull();
	}

	@Test
	void testCreateResources_publishesSetECCKeysEvent() {
		this.storageResourceConfig.createResources();

		Mockito.verify(this.publisher).publishEvent(Mockito.argThat((arg) -> {
			BDDAssertions.then(arg).isInstanceOf(SetECCKeysDto.class);
			SetECCKeysDto dto = (SetECCKeysDto) arg;
			BDDAssertions.then(dto.isSetECCKeys()).isTrue();
			return true;
		}));
	}

}
