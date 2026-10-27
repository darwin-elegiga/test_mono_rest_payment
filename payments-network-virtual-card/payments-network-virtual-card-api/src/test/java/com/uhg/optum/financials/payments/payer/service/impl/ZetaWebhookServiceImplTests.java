package com.uhg.optum.financials.payments.payer.service.impl;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.callback.ZetaWebhookDto;
import com.uhg.optum.financials.payments.payer.exception.QueuePublishException;
import com.uhg.optum.financials.payments.payer.exception.ServiceUnavailableException;
import com.uhg.optum.financials.payments.payer.service.StorageQueueProducer;
import com.uhg.optum.financials.payments.payer.util.DuplicateWebhookCheckUtil;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.Instant;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.RECEIVED;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.contains;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

@ExtendWith(MockitoExtension.class)
public class ZetaWebhookServiceImplTests {

	@InjectMocks
	ZetaWebhookServiceImpl zetaWebhookServiceImpl;

	@Mock
	StorageQueueProducer storageQueueProducer;

	@Mock
	LogUtility logUtility;

	@Mock
	DuplicateWebhookCheckUtil duplicateWebhookCheckUtil;

	@Mock
	ObjectMapper objectMapper;

	@Test
	void testProcessZetaWebhook() {

		Instant currentTime = Instant.now();
		ZetaWebhookDto<Object> zetaWebhookDto = new ZetaWebhookDto<>("1", "1.0", currentTime, "status", "type", null,
				null);

		BDDMockito.when(this.duplicateWebhookCheckUtil.checkForProcessedWebhook("1", "type", RECEIVED)).thenReturn(1);
		this.zetaWebhookServiceImpl.processZetaWebhook(zetaWebhookDto);
		verify(this.storageQueueProducer).sendVirtualCardEventToQueue(zetaWebhookDto);
	}

	@Test
	void testProcessZetaWebhookDuplicate() {

		Instant currentTime = Instant.now();
		ZetaWebhookDto<Object> zetaWebhookDto = new ZetaWebhookDto<>("1", "1.0", currentTime, "status", "type", null,
				null);

		BDDMockito.when(this.duplicateWebhookCheckUtil.checkForProcessedWebhook("1", "type", RECEIVED)).thenReturn(0);
		int result = this.zetaWebhookServiceImpl.processZetaWebhook(zetaWebhookDto);
		assertThat(result).isEqualTo(0);
		verify(this.storageQueueProducer, never()).sendVirtualCardEventToQueue(any());
	}

	@Test
	void testProcessZetaWebhook_QueuePublishException_RaisesP3Alert() {
		ZetaWebhookDto<Object> zetaWebhookDto = new ZetaWebhookDto<>("1", "1.0", Instant.now(), "status", "type", null,
				null);
		BDDMockito.when(this.duplicateWebhookCheckUtil.checkForProcessedWebhook("1", "type", RECEIVED)).thenReturn(1);
		BDDMockito.willThrow(new QueuePublishException("Queue unavailable", new RuntimeException("connection refused")))
			.given(this.storageQueueProducer)
			.sendVirtualCardEventToQueue(zetaWebhookDto);

		assertThatThrownBy(() -> this.zetaWebhookServiceImpl.processZetaWebhook(zetaWebhookDto))
			.isInstanceOf(ServiceUnavailableException.class)
			.hasMessageContaining("Queue unavailable");
		verify(this.logUtility).logMessageWithPriority(contains("Queue unavailable"), eq("error"), any(), eq("P3"));
	}

}
