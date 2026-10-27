package com.uhg.optum.financials.payments.payer.job.processor;

import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.AuditUtil;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.projection.PaymentSegment;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class SegmentProcessorTests {

	@InjectMocks
	private SegmentProcessor segmentProcessor;

	@Mock
	private LogUtility logUtility;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private WebhookClient webhookClient;

	@Mock
	private AuditUtil auditUtil;

	@Test
	void testProcess() {
		PaymentSegment paymentSegment = Mockito.mock(PaymentSegment.class);
		this.segmentProcessor.process(paymentSegment);
	}

}
