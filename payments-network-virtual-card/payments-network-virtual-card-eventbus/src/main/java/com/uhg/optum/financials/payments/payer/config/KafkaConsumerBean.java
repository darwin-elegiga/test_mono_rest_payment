package com.uhg.optum.financials.payments.payer.config;

import com.uhg.optum.financials.payments.payer.constant.EventTypeMapping;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.core.secrets.SecretCredentials;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import com.uhg.optum.financials.shared.eventbus.kafka.config.KafkaConsumerConfig;
import com.uhg.optum.financials.shared.eventbus.kafka.config.KafkaOptions;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnMissingBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.kafka.config.ConcurrentKafkaListenerContainerFactory;
import org.springframework.kafka.core.ConsumerFactory;
import org.springframework.kafka.listener.KafkaListenerErrorHandler;

import java.util.HashMap;
import java.util.Map;

/**
 * Kafka consumer bean configuration.
 *
 * @author v39, created on 08-July-2025
 */
@Configuration
@Slf4j
public class KafkaConsumerBean {

	@Value("${eventbus.env}")
	protected String eventBusEnv;

	@Value("${eventbus.topic}")
	protected String topic;

	@Value("${eventbus.consumerId}")
	protected String consumerId;

	@Value("${eventbus.bootstrap.servers}")
	protected String bootstrapServers;

	@Value("${application.name}")
	protected String applicationName;

	@Value("${eventbus.kafka.fullyQualifiedNamespace:}")
	protected String fullyQualifiedNamespace;

	@Autowired
	protected SecretService secretService;

	private Map<String, Class<?>> eventClassMapping;

	public KafkaConsumerBean() {
		this.eventClassMapping = new HashMap<>();
		for (EventTypeMapping classMapping : EventTypeMapping.values()) {
			this.eventClassMapping.put(classMapping.getEventType(), classMapping.getEventClass());
		}
	}

	@Bean
	@ConditionalOnMissingBean
	public ConcurrentKafkaListenerContainerFactory<String, EventBusEvent<?>> kafkaListenerContainerFactory() {
		KafkaOptions kafkaOptions = new KafkaOptions();

		// set kind based on variable eventBusEnv
		if (this.eventBusEnv.equalsIgnoreCase("LOCAL")) {
			kafkaOptions.setKind(KafkaOptions.KafkaKind.LOCAL);
		}
		else {
			kafkaOptions.setKind(KafkaOptions.KafkaKind.AZURE);
			if (this.fullyQualifiedNamespace != null && !this.fullyQualifiedNamespace.isBlank()) {
				kafkaOptions.setFullyQualifiedNamespace(this.fullyQualifiedNamespace);
			}
			else {
				SecretCredentials secretCredentials = this.secretService.getKafkaCredentials();
				kafkaOptions.setConnectionString(secretCredentials.getPassword());
			}
		}

		kafkaOptions.setBootstrapServers(this.bootstrapServers);
		kafkaOptions.setConsumerGroupId(this.consumerId);
		kafkaOptions.setApplicationName(this.applicationName); // Set applicationName here

		KafkaConsumerConfig kafkaConsumerConfig = new KafkaConsumerConfig();
		return kafkaConsumerConfig.kafkaListenerContainerFactory(kafkaOptions, this.eventClassMapping);
	}

	@Bean
	@ConditionalOnMissingBean
	public ConsumerFactory<String, EventBusEvent<?>> consumerFactory() {
		ConcurrentKafkaListenerContainerFactory<String, EventBusEvent<?>> listenerContainerFactory = kafkaListenerContainerFactory();

		return (ConsumerFactory<String, EventBusEvent<?>>) listenerContainerFactory.getConsumerFactory();
	}

	@Bean
	public KafkaListenerErrorHandler kafkaUnknownEventHandler() {
		return (msg, ex) -> {
			log.info("The event received by kafka is unrecognizable by virtualcard network. This has been ignored");
			return "SUCCESS";
		};
	}

}
