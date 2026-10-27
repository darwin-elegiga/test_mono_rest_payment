package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.shared.core.secrets.SecretCredentials;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.kafka.core.KafkaTemplate;

import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;

@ExtendWith(MockitoExtension.class)
class KafkaAdminProducerConfigTests {

	private SecretService secretService;

	private SecretCredentials secretCredentials;

	private KafkaAdminProducerConfig kafkaAdminProducer;

	@BeforeEach
	void setUp() {
		this.secretService = mock(SecretService.class);
		this.secretCredentials = mock(SecretCredentials.class);
		this.kafkaAdminProducer = new KafkaAdminProducerConfig("LOCAL", "test-topic", "localhost:9092", "",
				this.secretService);
	}

	@Test
	void testKafkaProducerConfig_LocalEnv() {
		EventBusProducer producer = this.kafkaAdminProducer.kafkaProducerConfig();
		Assertions.assertThat(producer).isNotNull();
	}

	@Test
	void testKafkaTemplate_LocalEnv() {
		KafkaTemplate<String, Object> template = this.kafkaAdminProducer.kafkaTemplate();
		Assertions.assertThat(template).isNotNull();
	}

	@Test
	void testKafkaProducerConfigWithConfiguredTopic() {
		KafkaAdminProducerConfig stgKafkaAdminProducer = new KafkaAdminProducerConfig("LOCAL", "administration-2",
				"localhost:9092", "", this.secretService);
		Assertions.assertThat(stgKafkaAdminProducer.kafkaProducerConfig()).isNotNull();
	}

	@Test
	void testKafkaTemplate_AzureWithFullyQualifiedNamespace() {
		this.kafkaAdminProducer = new KafkaAdminProducerConfig("AZURE", "test-topic", "localhost:9092",
				"virtual-card.servicebus.windows.net", this.secretService);

		KafkaTemplate<String, Object> template = this.kafkaAdminProducer.kafkaTemplate();

		Assertions.assertThat(template).isNotNull();
		then(this.secretService).should(never()).getKafkaCredentials();
	}

	@Test
	void testKafkaTemplate_AzureWithConnectionStringFallback() {
		given(this.secretService.getKafkaCredentials()).willReturn(this.secretCredentials);
		given(this.secretCredentials.getPassword()).willReturn(
				"Endpoint=sb://virtual-card.servicebus.windows.net/;SharedAccessKeyName=test-key-name;SharedAccessKey=test-key");
		this.kafkaAdminProducer = new KafkaAdminProducerConfig("AZURE", "test-topic", "localhost:9092", "",
				this.secretService);

		KafkaTemplate<String, Object> template = this.kafkaAdminProducer.kafkaTemplate();

		Assertions.assertThat(template).isNotNull();
		then(this.secretService).should().getKafkaCredentials();
		then(this.secretCredentials).should().getPassword();
	}

}
