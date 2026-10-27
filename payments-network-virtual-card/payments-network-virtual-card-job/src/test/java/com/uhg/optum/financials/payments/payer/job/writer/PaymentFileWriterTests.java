package com.uhg.optum.financials.payments.payer.job.writer;

import com.uhg.optum.financials.payments.payer.model.FileTracking;
import com.uhg.optum.financials.payments.payer.repository.FileTrackingRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.service.OutboundCorrelationResolver;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import com.uhg.optum.financials.shared.client.projection.PaymentSegment;
import com.uhg.optum.financials.shared.lib835.service.FileBuilderService;
import com.uhg.optum.financials.shared.lib835.service.util.FilePath;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.mockito.ArgumentCaptor;
import org.mockito.Mockito;
import org.springframework.batch.core.step.StepExecution;
import org.springframework.batch.infrastructure.item.Chunk;
import org.springframework.test.util.ReflectionTestUtils;

import java.util.Collections;
import java.util.HashSet;
import java.util.Optional;
import java.util.Set;

import static org.assertj.core.api.AssertionsForClassTypes.assertThat;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.times;

class PaymentFileWriterTests {

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private FileBuilderService paymentFileBuilder;

	@Mock
	private FileTrackingRepository fileTrackingRepository;

	@Mock
	private BatchDetailService batchDetailService;

	@Mock
	private VirtualCardUtil virtualCardUtil;

	@Mock
	private OutboundCorrelationResolver outboundCorrelationResolver;

	@Mock
	private Chunk<PaymentSegment> chunk;

	@Mock
	private StepExecution stepExecution;

	@Mock
	private FilePath filePath;

	@Mock
	private FileTracking fileTracking;

	@InjectMocks
	private PaymentFileWriter paymentFileWriter;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
		ReflectionTestUtils.setField(this.paymentFileWriter, "fileGenerationPath", "/tmp/test");
		ReflectionTestUtils.setField(this.paymentFileWriter, "jobParam", "{\"correlationId\":\"cid123\"}");
		Mockito.lenient()
			.when(this.outboundCorrelationResolver.resolveActualCorrelationId(Mockito.anyString()))
			.thenAnswer((invocation) -> invocation.getArgument(0));
	}

	@Test
	void beforeStep_shouldInitializeFields() {
		Mockito.when(this.outboundCorrelationResolver.resolveActualCorrelationId("cid123")).thenReturn("orig-cid");
		this.paymentFileWriter.beforeStep(this.stepExecution);
		assertThat(this.paymentFileWriter).isNotNull();
		Mockito.verify(this.outboundCorrelationResolver).resolveActualCorrelationId("cid123");
	}

	@Test
	void beforeStep_shouldFallBackToProcessingCorrelationIdWhenResolverReturnsBlank() {
		Mockito.when(this.outboundCorrelationResolver.resolveActualCorrelationId("cid123")).thenReturn(" ");
		this.paymentFileWriter.beforeStep(this.stepExecution);
		FilePath createdFilePath = (FilePath) ReflectionTestUtils.getField(this.paymentFileWriter, "filePath");
		assertThat(createdFilePath).isNotNull();
		Mockito.verify(this.outboundCorrelationResolver).resolveActualCorrelationId("cid123");
	}

	@Test
	void write_shouldProcessNonEmptyChunk() {
		PaymentSegment paymentSegment = org.mockito.Mockito.mock(PaymentSegment.class);
		given(this.chunk.getItems()).willReturn(Collections.singletonList(paymentSegment));
		given(paymentSegment.getPaymentId()).willReturn("pid1");
		Set<String> fileCreatedIds = new HashSet<>();
		fileCreatedIds.add("pid1");
		given(this.paymentFileBuilder.writePaymentsToFile(org.mockito.ArgumentMatchers.any(FilePath.class),
				org.mockito.ArgumentMatchers.anyList()))
			.willReturn(fileCreatedIds);
		org.mockito.BDDMockito.willDoNothing()
			.given(this.paymentRequestRepository)
			.updateStatusByIdPaymentIdIn(org.mockito.ArgumentMatchers.anyString(),
					org.mockito.ArgumentMatchers.anySet());
		org.mockito.BDDMockito.willDoNothing()
			.given(this.virtualCardUtil)
			.savePaymentRequestHistoryForIds(org.mockito.ArgumentMatchers.anySet());
		given(this.fileTrackingRepository.findByCorrelationId(org.mockito.ArgumentMatchers.anyString()))
			.willReturn(Optional.empty());
		given(this.filePath.getPathWithName()).willReturn("/tmp/test/file.txt");
		// Set filePath field using ReflectionTestUtils
		ReflectionTestUtils.setField(this.paymentFileWriter, "filePath", this.filePath);
		this.paymentFileWriter.write(this.chunk);
		then(this.paymentFileBuilder).should(times(1))
			.writePaymentsToFile(org.mockito.ArgumentMatchers.any(FilePath.class),
					org.mockito.ArgumentMatchers.anyList());
		then(this.paymentRequestRepository).should(times(1))
			.updateStatusByIdPaymentIdIn(org.mockito.ArgumentMatchers.anyString(),
					org.mockito.ArgumentMatchers.anySet());
		then(this.virtualCardUtil).should(times(1))
			.savePaymentRequestHistoryForIds(org.mockito.ArgumentMatchers.anySet());
		then(this.batchDetailService).should(times(1)).updateBatchDetailStatus(eq("cid123"), eq("R"), eq("I"));
		then(this.fileTrackingRepository).should(times(1)).save(org.mockito.ArgumentMatchers.any(FileTracking.class));
		ArgumentCaptor<FileTracking> fileTrackingCaptor = ArgumentCaptor.forClass(FileTracking.class);
		then(this.fileTrackingRepository).should().save(fileTrackingCaptor.capture());
		assertThat(fileTrackingCaptor.getValue().getCompanionStatus()).isEqualTo("PENDING");
		assertThat(fileTrackingCaptor.getValue().getPaymentActivityStatus()).isEqualTo("PENDING");
	}

	@Test
	void write_shouldSkipEmptyChunk() {
		given(this.chunk.getItems()).willReturn(Collections.emptyList());
		this.paymentFileWriter.write(this.chunk);
		then(this.paymentFileBuilder).should(times(0))
			.writePaymentsToFile(org.mockito.ArgumentMatchers.any(FilePath.class),
					org.mockito.ArgumentMatchers.anyList());
		then(this.paymentRequestRepository).should(times(0))
			.updateStatusByIdPaymentIdIn(org.mockito.ArgumentMatchers.anyString(),
					org.mockito.ArgumentMatchers.anySet());
		then(this.batchDetailService).should(times(0))
			.updateBatchDetailStatus(org.mockito.ArgumentMatchers.anyString(), org.mockito.ArgumentMatchers.anyString(),
					org.mockito.ArgumentMatchers.anyString());
		then(this.fileTrackingRepository).should(times(0)).save(org.mockito.ArgumentMatchers.any(FileTracking.class));
	}

}
