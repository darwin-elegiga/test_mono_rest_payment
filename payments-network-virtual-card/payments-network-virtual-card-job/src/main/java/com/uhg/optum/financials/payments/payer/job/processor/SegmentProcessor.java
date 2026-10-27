package com.uhg.optum.financials.payments.payer.job.processor;

import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.client.projection.PaymentSegment;
import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.core.configuration.annotation.JobScope;
import org.springframework.batch.infrastructure.item.ItemProcessor;
import org.springframework.stereotype.Component;

import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.DEBUG;

@Slf4j
@Component
@JobScope
public class SegmentProcessor implements ItemProcessor<PaymentSegment, PaymentSegment> {

	private final LogUtility logUtility;

	public SegmentProcessor(LogUtility logUtility) {
		this.logUtility = logUtility;
	}

	@Override
	public PaymentSegment process(PaymentSegment paymentRequest) {
		this.logUtility.logMessage("Processing data: " + paymentRequest.getStSeSegment(), DEBUG, log);
		return paymentRequest;
	}

}
