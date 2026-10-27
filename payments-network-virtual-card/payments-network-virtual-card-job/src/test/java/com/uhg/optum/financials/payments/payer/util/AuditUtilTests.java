package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.shared.eventbus.kafka.audit.implementation.KafkaAuditPublisher;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class AuditUtilTests {

	@Test
	void testConstructor() {
		KafkaAuditPublisher kafkaAuditPublisher = Mockito.mock(KafkaAuditPublisher.class);
		AuditUtil auditUtils = new AuditUtil(kafkaAuditPublisher, "test", "test", "test", "test", "test");
		Assertions.assertThatNoException().isThrownBy(() -> auditUtils.sendErrorEvent("test"));
	}

}
