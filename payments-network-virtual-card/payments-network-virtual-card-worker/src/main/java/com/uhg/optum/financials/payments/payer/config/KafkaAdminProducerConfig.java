package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.shared.core.secrets.SecretCredentials;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import com.uhg.optum.financials.shared.eventbus.kafka.config.KafkaOptions;
import com.uhg.optum.financials.shared.eventbus.kafka.config.KafkaProducerConfig;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import com.uhg.optum.financials.shared.eventbus.kafka.service.implementation.KafkaEventBusProducer;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.kafka.core.KafkaTemplate;

@Configuration
public class KafkaAdminProducerConfig {

	private final String eventBusEnv;

	private final String topic;

	private final String bootstrapServers;

	private final String fullyQualifiedNamespace;

	private final SecretService secretService;

	public KafkaAdminProducerConfig(@Value("${eventbus.producer.env}") String eventBusEnv,
			@Value("${eventbus.producer.topic}") String topic,
			@Value("${eventbus.producer.bootstrap.servers}") String bootstrapServers,
			@Value("${eventbus.kafka.fullyQualifiedNamespace:}") String fullyQualifiedNamespace,
			SecretService secretService) {
		this.eventBusEnv = eventBusEnv;
		this.topic = topic;
		this.bootstrapServers = bootstrapServers;
		this.fullyQualifiedNamespace = fullyQualifiedNamespace;
		this.secretService = secretService;
	}

	@Bean("adminProducer")
	public EventBusProducer kafkaProducerConfig() {
		return new KafkaEventBusProducer(this.kafkaTemplate(), this.topic);
	}

	public KafkaTemplate<String, Object> kafkaTemplate() {
		KafkaOptions kafkaOptions = new KafkaOptions();
		if (this.eventBusEnv.equalsIgnoreCase("LOCAL")) {
			kafkaOptions.setKind(KafkaOptions.KafkaKind.LOCAL);
		}
		else {
			kafkaOptions.setKind(KafkaOptions.KafkaKind.AZURE);
			String configuredNamespace = (this.fullyQualifiedNamespace == null) ? ""
					: this.fullyQualifiedNamespace.trim();
			if (!configuredNamespace.isBlank()) {
				kafkaOptions.setFullyQualifiedNamespace(configuredNamespace);
			}
			else {
				SecretCredentials secretCredentials = this.secretService.getKafkaCredentials();
				kafkaOptions.setConnectionString(secretCredentials.getPassword());
			}
		}

		kafkaOptions.setBootstrapServers(this.bootstrapServers);
		kafkaOptions.setConsumerGroupId(null);
		KafkaProducerConfig kafkaProducerConfig = new KafkaProducerConfig();
		return kafkaProducerConfig.kafkaTemplate(kafkaOptions);
	}

}
