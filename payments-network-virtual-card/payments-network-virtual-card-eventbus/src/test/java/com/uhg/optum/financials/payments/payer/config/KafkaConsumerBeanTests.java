package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.core.secrets.SecretCredentials;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.mockito.Mockito;
import org.springframework.kafka.config.ConcurrentKafkaListenerContainerFactory;
import org.springframework.kafka.core.ConsumerFactory;
import org.springframework.kafka.listener.ListenerExecutionFailedException;
import org.springframework.messaging.Message;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;

public class KafkaConsumerBeanTests {

	private KafkaConsumerBean kafkaConsumerBean;

	private SecretService secretService;

	@BeforeEach
	void setUp() {
		this.secretService = mock(SecretService.class);
		this.kafkaConsumerBean = new KafkaConsumerBean();
		this.kafkaConsumerBean.secretService = this.secretService;
		this.kafkaConsumerBean.eventBusEnv = "LOCAL";
		this.kafkaConsumerBean.bootstrapServers = "localhost:9092";
		this.kafkaConsumerBean.consumerId = "test-consumer";
		this.kafkaConsumerBean.applicationName = "test-app";
		this.kafkaConsumerBean.fullyQualifiedNamespace = "";
	}

	@Test
	@DisplayName("should initialize eventClassMapping in constructor via bean creation")
	void shouldInitializeEventClassMappingViaBean() {
		// Given
		// KafkaConsumerBean is initialized in setUp
		// When
		ConcurrentKafkaListenerContainerFactory<String, EventBusEvent<?>> factory = this.kafkaConsumerBean
			.kafkaListenerContainerFactory();
		// Then
		assertThat(factory).isNotNull();
	}

	@Test
	@DisplayName("should create kafkaListenerContainerFactory for LOCAL env")
	void shouldCreateKafkaListenerContainerFactoryLocal() {
		// Given
		this.kafkaConsumerBean.eventBusEnv = "LOCAL";
		// When
		ConcurrentKafkaListenerContainerFactory<String, EventBusEvent<?>> factory = this.kafkaConsumerBean
			.kafkaListenerContainerFactory();
		// Then
		assertThat(factory).isNotNull();
	}

	@Test
	@DisplayName("should create kafkaListenerContainerFactory for AZURE env")
	void shouldCreateKafkaListenerContainerFactoryAzure() {
		// Given
		this.kafkaConsumerBean.eventBusEnv = "AZURE";
		SecretCredentials credentials = mock(SecretCredentials.class);
		given(this.secretService.getKafkaCredentials()).willReturn(credentials);
		String validConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=somekey;EntityPath=test-entity";
		given(credentials.getPassword()).willReturn(validConnectionString);
		// When
		ConcurrentKafkaListenerContainerFactory<String, EventBusEvent<?>> factory = this.kafkaConsumerBean
			.kafkaListenerContainerFactory();
		// Then
		assertThat(factory).isNotNull();
	}

	@Test
	@DisplayName("should create kafkaListenerContainerFactory for AZURE env with fully qualified namespace")
	void shouldCreateKafkaListenerContainerFactoryAzureWithFullyQualifiedNamespace() {
		// Given
		this.kafkaConsumerBean.eventBusEnv = "AZURE";
		this.kafkaConsumerBean.fullyQualifiedNamespace = "virtual-card.servicebus.windows.net";
		// When
		ConcurrentKafkaListenerContainerFactory<String, EventBusEvent<?>> factory = this.kafkaConsumerBean
			.kafkaListenerContainerFactory();
		// Then
		assertThat(factory).isNotNull();
		verify(this.secretService, never()).getKafkaCredentials();
	}

	@Test
	@DisplayName("should create consumerFactory bean")
	void shouldCreateConsumerFactory() {
		ConsumerFactory<String, EventBusEvent<?>> consumerFactory = this.kafkaConsumerBean.consumerFactory();
		assertThat(consumerFactory).isNotNull();
	}

	@Test
	void testKafkaUnknownEventHandler() {
		Message<String> message = Mockito.mock(Message.class);
		String response = this.kafkaConsumerBean.kafkaUnknownEventHandler()
			.handleError(message, new ListenerExecutionFailedException("ERROR"))
			.toString();
		Assertions.assertThat(response).isEqualTo("SUCCESS");
	}

}
