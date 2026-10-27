package com.uhg.optum.financials.payments.payer.job.writer;

import com.uhg.optum.financials.payments.payer.model.FileTracking;
import com.uhg.optum.financials.payments.payer.repository.FileTrackingRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import com.uhg.optum.financials.shared.client.projection.PaymentSegment;
import com.uhg.optum.financials.shared.lib835.segment.FundingSegment;
import com.uhg.optum.financials.shared.lib835.service.FileBuilderService;
import com.uhg.optum.financials.shared.lib835.service.util.FilePath;
import com.uhg.optum.financials.payments.payer.service.OutboundCorrelationResolver;
import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.core.annotation.AfterStep;
import org.springframework.batch.core.annotation.BeforeStep;
import org.springframework.batch.core.configuration.annotation.JobScope;
import org.springframework.batch.core.step.StepExecution;
import org.springframework.batch.infrastructure.item.Chunk;
import org.springframework.batch.infrastructure.item.ItemWriter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Propagation;
import org.springframework.transaction.annotation.Transactional;

import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.TreeSet;
import java.util.stream.Collectors;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_835_CREATED;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_PROGRESS;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.CORRELATION_ID;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.FUNDING_DATA_KEY;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.PARAM;
import static com.uhg.optum.financials.payments.payer.constant.JobConstant.PAYMENT_ID_KEY;
import static com.uhg.optum.financials.payments.payer.enums.Status.PC;
import static com.uhg.optum.financials.payments.payer.util.ParameterProvider.getValue;
import static com.uhg.optum.financials.shared.client.util.DateTimeUtil.toNewYorkZoneYYYYMMDD;

@Slf4j
@Component
@Transactional(propagation = Propagation.REQUIRED)
@JobScope
public class PaymentFileWriter implements ItemWriter<PaymentSegment> {

	@Value("${directory.file.generation-path}")
	private String fileGenerationPath;

	@Value("#{jobParameters['" + PARAM + "']}")
	private String jobParam;

	private final FileBuilderService paymentFileBuilder;

	private final PaymentRequestRepository paymentRequestRepository;

	private final FileTrackingRepository fileTrackingRepository;

	private final VirtualCardUtil virtualCardUtil;

	private final OutboundCorrelationResolver outboundCorrelationResolver;

	public PaymentFileWriter(PaymentRequestRepository paymentRequestRepository, FileBuilderService paymentFileBuilder,
			FileTrackingRepository fileTrackingRepository, VirtualCardUtil virtualCardUtil,
			BatchDetailService batchDetailService, OutboundCorrelationResolver outboundCorrelationResolver) {
		this.paymentFileBuilder = paymentFileBuilder;
		this.paymentRequestRepository = paymentRequestRepository;
		this.fileTrackingRepository = fileTrackingRepository;
		this.virtualCardUtil = virtualCardUtil;
		this.batchDetailService = batchDetailService;
		this.outboundCorrelationResolver = outboundCorrelationResolver;
	}

	private final Set<String> fileCreatedPayer835Ids = new HashSet<>();

	private final Set<String> paymentIds = new HashSet<>();

	private final TreeSet<String> networkPaymentIds = new TreeSet<>();

	private FilePath filePath;

	private static final String STORAGE_URL_TIME_TO_LIVE = "30";

	private static final String STATUS_PENDING = "PENDING";

	private final BatchDetailService batchDetailService;

	@BeforeStep
	public void beforeStep(StepExecution step) {
		String processingCorrelationId = getValue(this.jobParam, CORRELATION_ID);
		String originalCorrelationId = this.outboundCorrelationResolver
			.resolveActualCorrelationId(processingCorrelationId);
		if (originalCorrelationId == null || originalCorrelationId.isBlank()) {
			originalCorrelationId = processingCorrelationId;
		}
		String old835FileDate = toNewYorkZoneYYYYMMDD();
		this.fileCreatedPayer835Ids.clear();
		this.paymentIds.clear();
		this.networkPaymentIds.clear();
		this.filePath = FilePath.defaultBuilder(this.fileGenerationPath, false, old835FileDate, originalCorrelationId)
			.build();
	}

	@Override
	public void write(Chunk<? extends PaymentSegment> chunk) {
		log.info("Writing payment segments to file: {}, {}", this.fileGenerationPath, this.hashCode());
		String correlationId = getValue(this.jobParam, CORRELATION_ID);
		List<PaymentSegment> paymentSegments = (List<PaymentSegment>) chunk.getItems();
		// Create payment file and store the payment835Id for which payment is created
		if (!paymentSegments.isEmpty()) {
			Set<String> fileCreatedForIds = this.paymentFileBuilder.writePaymentsToFile(this.filePath, paymentSegments);
			this.fileCreatedPayer835Ids.addAll(fileCreatedForIds);

			// Update the status of the payment request to PC
			Set<String> processedReqIds = paymentSegments.stream()
				.map(PaymentSegment::getPaymentId)
				.collect(Collectors.toSet());

			this.paymentRequestRepository.updateStatusByIdPaymentIdIn(PC.name(), processedReqIds);
			this.virtualCardUtil.savePaymentRequestHistoryForIds(processedReqIds);
			this.batchDetailService.updateBatchDetailStatus(correlationId, BATCH_DETAIL_STATUS_835_CREATED,
					BATCH_DETAIL_STATUS_PROGRESS);
			setFileTrackingInPaymentRequests(correlationId, this.filePath.getPathWithName());
			// store the paymentIds for which payment file is created
			this.paymentIds.addAll(processedReqIds);
		}
	}

	private void setFileTrackingInPaymentRequests(String correlationId, String fileNameWithPath) {
		FileTracking fileTracking = this.fileTrackingRepository.findByCorrelationId(correlationId)
			.orElseGet(FileTracking::new);
		fileTracking.setCorrelationId(correlationId);
		fileTracking.set_835(fileNameWithPath);
		fileTracking.setCompanionStatus(STATUS_PENDING);
		fileTracking.setPaymentActivityStatus(STATUS_PENDING);
		this.fileTrackingRepository.save(fileTracking);

	}

	@AfterStep
	public void afterStep(StepExecution step) {
		log.info("After step processing for payment file writer: {}, {}", this.paymentFileBuilder.hashCode(),
				this.hashCode());
		List<FundingSegment> fundingSegments = this.paymentFileBuilder.markAsEndOfFile(this.fileGenerationPath,
				this.fileCreatedPayer835Ids);
		step.getJobExecution().getExecutionContext().put(FUNDING_DATA_KEY, fundingSegments);

		step.getJobExecution().getExecutionContext().put(PAYMENT_ID_KEY, this.paymentIds);
	}

}
