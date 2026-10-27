package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.dto.CorrelatedPaymentDetail;
import com.uhg.optum.financials.payments.payer.job.processor.SegmentProcessor;
import com.uhg.optum.financials.payments.payer.job.reader.SegmentReader;
import com.uhg.optum.financials.payments.payer.job.writer.PaymentFileWriter;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.batch.core.job.Job;
import org.springframework.batch.core.job.parameters.JobParameters;
import org.springframework.batch.core.repository.JobRepository;
import org.springframework.batch.core.step.tasklet.TaskletStep;
import org.springframework.beans.factory.ObjectProvider;
import org.springframework.transaction.PlatformTransactionManager;

import java.math.BigDecimal;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.BDDMockito.given;
import static org.mockito.Mockito.mock;

class File835JobTests {

	private File835Job file835Job;

	private JobRepository jobRepository;

	private PlatformTransactionManager transactionManager;

	private ObjectProvider<SegmentReader> segmentReaderProvider;

	private ObjectProvider<SegmentProcessor> segmentProcessorProvider;

	private ObjectProvider<PaymentFileWriter> paymentFileWriterProvider;

	private SegmentReader segmentReader;

	private SegmentProcessor segmentProcessor;

	private PaymentFileWriter paymentFileWriter;

	private LogUtility logUtility;

	@BeforeEach
	void setUp() {
		this.jobRepository = mock(JobRepository.class);
		this.transactionManager = mock(PlatformTransactionManager.class);

		// Create mock beans
		this.segmentReader = mock(SegmentReader.class);
		this.segmentProcessor = mock(SegmentProcessor.class);
		this.paymentFileWriter = mock(PaymentFileWriter.class);

		// Create mock providers
		this.segmentReaderProvider = mock(ObjectProvider.class);
		this.segmentProcessorProvider = mock(ObjectProvider.class);
		this.paymentFileWriterProvider = mock(ObjectProvider.class);

		// Configure providers to return the mock beans
		given(this.segmentReaderProvider.getObject()).willReturn(this.segmentReader);
		given(this.segmentProcessorProvider.getObject()).willReturn(this.segmentProcessor);
		given(this.paymentFileWriterProvider.getObject()).willReturn(this.paymentFileWriter);

		this.logUtility = mock(LogUtility.class);
		this.file835Job = new File835Job(this.jobRepository, this.transactionManager, this.segmentReaderProvider,
				this.segmentProcessorProvider, this.paymentFileWriterProvider, this.logUtility) {
		};
	}

	@Test
	void testBuildJobParam() {
		// given
		CorrelatedPaymentDetail cpr = correlatedPaymentDetail("corrId", 10L, BigDecimal.valueOf(1000));

		// when
		JobParameters params = this.file835Job.buildJobParam("jobId", cpr);

		// then
		assertThat(params.getString("jobParam")).isNotNull();
	}

	@Test
	void testConsolidatedFileJob() {
		// when
		Job job = this.file835Job.consolidatedFileJob("testJob", 5);

		// then
		assertThat(job).isNotNull();
		assertThat(job.getName()).isEqualTo("testJob");
	}

	@Test
	void testPrivateCleanupTask() throws Exception {
		// when
		TaskletStep step = (TaskletStep) invokePrivateMethod("cleanupTask");

		// then
		assertThat(step.getName()).isEqualTo("CLEANUP_DATA");
	}

	private Object invokePrivateMethod(String name) throws Exception {
		var m = File835Job.class.getDeclaredMethod(name);
		m.setAccessible(true);
		return m.invoke(this.file835Job);
	}

	private CorrelatedPaymentDetail correlatedPaymentDetail(String correlationId, Long numberOfPayments,
			BigDecimal totalAmount) {
		return new CorrelatedPaymentDetail() {
			@Override
			public String getCorrelationId() {
				return correlationId;
			}

			@Override
			public Long getNumberOfPayments() {
				return numberOfPayments;
			}

			@Override
			public BigDecimal getTotalAmount() {
				return totalAmount;
			}
		};
	}

}
