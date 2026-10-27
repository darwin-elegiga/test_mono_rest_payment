package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.dto.CorrelatedPaymentDetail;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelatedBatchDetail;
import com.uhg.optum.financials.payments.payer.enums.Status;
import com.uhg.optum.financials.payments.payer.enums.VirtualCardState;
import com.uhg.optum.financials.payments.payer.job.processor.SegmentProcessor;
import com.uhg.optum.financials.payments.payer.job.reader.SegmentReader;
import com.uhg.optum.financials.payments.payer.job.writer.PaymentFileWriter;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.MockedConstruction;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.batch.core.ExitStatus;
import org.springframework.batch.core.configuration.JobRegistry;
import org.springframework.batch.core.job.JobExecution;
import org.springframework.batch.core.launch.support.TaskExecutorJobOperator;
import org.springframework.batch.core.repository.JobRepository;
import org.springframework.beans.factory.ObjectProvider;
import org.springframework.scheduling.concurrent.ThreadPoolTaskExecutor;
import org.springframework.test.util.ReflectionTestUtils;
import org.springframework.transaction.PlatformTransactionManager;

import java.math.BigDecimal;
import java.util.Collections;
import java.util.List;
import java.util.concurrent.ThreadPoolExecutor;

import static com.uhg.optum.financials.payments.payer.constant.JobConstant.PAYMENT_COUNT_NOT_MATCH;
import static org.mockito.ArgumentMatchers.any;

@Slf4j
@ExtendWith(MockitoExtension.class)
class Payment835JobTests {

	private Payment835Job payment835Job;

	@Mock
	private JobRepository jobRepository;

	@Mock
	private PlatformTransactionManager transactionManager;

	@Mock
	private ObjectProvider<SegmentReader> segmentReaderProvider;

	@Mock
	private ObjectProvider<SegmentProcessor> segmentProcessorProvider;

	@Mock
	private ObjectProvider<PaymentFileWriter> paymentFileWriterProvider;

	@Mock
	private SegmentReader segmentReader;

	@Mock
	private SegmentProcessor segmentProcessor;

	@Mock
	private PaymentFileWriter paymentFileWriter;

	@Mock
	private BatchDetailService batchDtlSvc;

	@Mock
	private PaymentRequestRepository paymentReqRepo;

	@Mock
	private LogUtility logUtility;

	@Mock
	private JobExecution jobExecution;

	@Mock
	private ExitStatus exitStatus;

	@Mock
	private ThreadPoolTaskExecutor taskExecutor;

	@Mock
	private ThreadPoolExecutor threadPoolExecutor;

	@Mock
	private JobRegistry jobRegistry;

	private static MockedConstruction<TaskExecutorJobOperator> jobLauncherConstruction;

	@BeforeAll
	static void beforeAll() {
		jobLauncherConstruction = Mockito.mockConstruction(TaskExecutorJobOperator.class, (mock, context) -> {
			Mockito.doNothing().when(mock).setJobRepository(any());
			Mockito.doNothing().when(mock).setTaskExecutor(any());
			Mockito.doNothing().when(mock).afterPropertiesSet();
		});
	}

	@AfterAll
	static void afterAll() {
		if (jobLauncherConstruction != null) {
			jobLauncherConstruction.close();
		}
	}

	@BeforeEach
	void setUp() {
		// Configure providers to return mock beans - using lenient() as these are not
		// always used in all tests
		Mockito.lenient().when(this.segmentReaderProvider.getObject()).thenReturn(this.segmentReader);
		Mockito.lenient().when(this.segmentProcessorProvider.getObject()).thenReturn(this.segmentProcessor);
		Mockito.lenient().when(this.paymentFileWriterProvider.getObject()).thenReturn(this.paymentFileWriter);

		// Create Payment835Job instance
		this.payment835Job = new Payment835Job(this.jobRepository, this.transactionManager, this.segmentReaderProvider,
				this.segmentProcessorProvider, this.paymentFileWriterProvider, this.batchDtlSvc, this.paymentReqRepo,
				this.logUtility, this.jobRegistry, this.taskExecutor);

		ReflectionTestUtils.setField(this.payment835Job, "alertTimeFor835File", 10);
		ReflectionTestUtils.setField(this.payment835Job, "chunkSize", 10);
	}

	@Test
	void testRunJobFailed() {
		Mockito.when(this.batchDtlSvc.getCorrelatedBatch(Mockito.anyString())).thenReturn(Collections.emptyList());
		String result = this.payment835Job.run();
		Assertions.assertThat(result).isEmpty();
	}

	@Test
	void testRunJobSuccess() {
		CorrelatedBatchDetail batch = this.correlatedPayment("corr-1", 2L, BigDecimal.valueOf(100));
		CorrelatedPaymentDetail cpr = this.correlatedPaymentRequest("corr-1", 2L, BigDecimal.valueOf(100));

		Mockito.when(this.batchDtlSvc.getCorrelatedBatch(any())).thenReturn(List.of(batch));
		Mockito
			.when(this.paymentReqRepo.findCorrelatedPaymentRequest("corr-1", Status.PI.name(),
					VirtualCardState.CREATED.name()))
			.thenReturn(cpr);

		String result = this.payment835Job.run();
		Assertions.assertThat(result).isNotNull();
	}

	@Test
	void testCheckEligibilityAndSubmitJob_PaymentCountNotMatch() {
		CorrelatedBatchDetail batch = this.correlatedPayment("corr-2", 2L, BigDecimal.valueOf(100));
		CorrelatedPaymentDetail cpr = this.correlatedPaymentRequest("corr-2", 3L, BigDecimal.valueOf(100));

		Mockito.when(this.batchDtlSvc.getCorrelatedBatch(any())).thenReturn(List.of(batch));
		Mockito
			.when(this.paymentReqRepo.findCorrelatedPaymentRequest("corr-2", Status.PI.name(),
					VirtualCardState.CREATED.name()))
			.thenReturn(cpr);

		Mockito.when(this.batchDtlSvc.timeDifferenceInHours("corr-2")).thenReturn(11L);

		String result = this.payment835Job.run();

		Assertions.assertThat(result).isEmpty();
		Mockito.verify(this.logUtility)
			.logMessage(Mockito.contains(PAYMENT_COUNT_NOT_MATCH), Mockito.anyString(), Mockito.any());

	}

	@Test
	void testCheckEligibilityAndSubmitJob_PaymentCountNotMatchWithinThreshold() {
		CorrelatedBatchDetail batch = this.correlatedPayment("corr-3", 2L, BigDecimal.valueOf(100));
		CorrelatedPaymentDetail cpr = this.correlatedPaymentRequest("corr-3", 3L, BigDecimal.valueOf(100));

		Mockito.when(this.batchDtlSvc.getCorrelatedBatch(any())).thenReturn(List.of(batch));
		Mockito
			.when(this.paymentReqRepo.findCorrelatedPaymentRequest("corr-3", Status.PI.name(),
					VirtualCardState.CREATED.name()))
			.thenReturn(cpr);

		Mockito.when(this.batchDtlSvc.timeDifferenceInHours("corr-3")).thenReturn(5L);

		String result = this.payment835Job.run();

		Assertions.assertThat(result).isEmpty();
		Mockito.verify(this.logUtility)
			.logMessage(Mockito.contains("Time difference is less than"), Mockito.anyString(), Mockito.any());
	}

	@Test
	void testCheckEligibilityAndSubmitJob_AmountNotMatch() {
		CorrelatedBatchDetail batch = this.correlatedPayment("corr-4", 2L, BigDecimal.valueOf(100));
		CorrelatedPaymentDetail cpr = this.correlatedPaymentRequest("corr-4", 2L, BigDecimal.valueOf(200));

		Mockito.when(this.batchDtlSvc.getCorrelatedBatch(any())).thenReturn(List.of(batch));
		Mockito
			.when(this.paymentReqRepo.findCorrelatedPaymentRequest("corr-4", Status.PI.name(),
					VirtualCardState.CREATED.name()))
			.thenReturn(cpr);

		String result = this.payment835Job.run(false);

		Assertions.assertThat(result).isEmpty();
	}

	@Test
	void testCheckEligibilityAndSubmitJob_NullCorrelatedPaymentRequest() {
		CorrelatedBatchDetail batch = this.correlatedPayment("corr-5", 2L, BigDecimal.valueOf(100));

		Mockito.when(this.batchDtlSvc.getCorrelatedBatch(any())).thenReturn(List.of(batch));
		Mockito
			.when(this.paymentReqRepo.findCorrelatedPaymentRequest("corr-5", Status.PI.name(),
					VirtualCardState.CREATED.name()))
			.thenReturn(null);

		Mockito.when(this.batchDtlSvc.timeDifferenceInHours("corr-5")).thenReturn(12L);

		String result = this.payment835Job.run();

		Assertions.assertThat(result).isEmpty();
	}

	@Test
	void testSubmitJob_Synchronization() {
		CorrelatedBatchDetail batch = this.correlatedPayment("corr-6", 2L, BigDecimal.valueOf(100));
		CorrelatedPaymentDetail cpr = this.correlatedPaymentRequest("corr-6", 2L, BigDecimal.valueOf(100));

		Mockito.when(this.batchDtlSvc.getCorrelatedBatch(any())).thenReturn(List.of(batch));
		Mockito
			.when(this.paymentReqRepo.findCorrelatedPaymentRequest("corr-6", Status.PI.name(),
					VirtualCardState.CREATED.name()))
			.thenReturn(cpr);

		String result = this.payment835Job.run(false);

		Assertions.assertThat(result).isNotEmpty();
	}

	@Test
	void testSubmitJob_Async() {
		CorrelatedBatchDetail batch = this.correlatedPayment("corr-6", 2L, BigDecimal.valueOf(100));
		CorrelatedBatchDetail batch2 = this.correlatedPayment("corr-7", 3L, BigDecimal.valueOf(200));

		CorrelatedPaymentDetail cpr = this.correlatedPaymentRequest("corr-6", 2L, BigDecimal.valueOf(100));
		CorrelatedPaymentDetail cpr2 = this.correlatedPaymentRequest("corr-7", 3L, BigDecimal.valueOf(200));

		Mockito.when(this.batchDtlSvc.getCorrelatedBatch(any())).thenReturn(List.of(batch, batch2));
		Mockito
			.when(this.paymentReqRepo.findCorrelatedPaymentRequest("corr-6", Status.PI.name(),
					VirtualCardState.CREATED.name()))
			.thenReturn(cpr);
		Mockito
			.when(this.paymentReqRepo.findCorrelatedPaymentRequest("corr-7", Status.PI.name(),
					VirtualCardState.CREATED.name()))
			.thenReturn(cpr2);

		String result = this.payment835Job.run();

		Assertions.assertThat(result).isNotEmpty();
	}

	@Test
	void testBuildJobParameters_WithoutRemit() {
		CorrelatedPaymentDetail cpr = this.correlatedPaymentRequest("corr-8", null, null);

		Object params = ReflectionTestUtils.invokeMethod(this.payment835Job, "buildJobParam", "jobId-8", cpr);

		Assertions.assertThat(params).isNotNull();
	}

	private CorrelatedBatchDetail correlatedPayment(String correlationId, Long numberOfPayments,
			BigDecimal totalAmount) {
		return new CorrelatedBatchDetail() {
			@Override
			public Long getNumberOfPayments() {
				return numberOfPayments;
			}

			@Override
			public String getCorrelationId() {
				return correlationId;
			}

			@Override
			public BigDecimal getTotalAmount() {
				return totalAmount;
			}

		};
	}

	private CorrelatedPaymentDetail correlatedPaymentRequest(String correlationId, Long numberOfPayments,
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
