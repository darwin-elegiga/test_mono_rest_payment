package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.PaymentRequestStatusAmountCountDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentRequestTotalDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchCorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchDetailUpdateRequestDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchUpdateTimeRangeRequestDto;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelatedBatchDetail;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.model.BatchDetail;
import com.uhg.optum.financials.payments.payer.repository.BatchDetailRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.BatchDetailService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.Duration;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.stream.Collectors;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_INITIALIZE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUALCARD;

@Slf4j
@Service
public class BatchDetailServiceImpl implements BatchDetailService {

	private static final String RE_PREFERENCE_BATCH_ID_PREFIX = "RRF";

	private final BatchDetailRepository batchDetailRepository;

	private final LogUtility logUtility;

	private final PaymentRequestRepository paymentRequestRepository;

	public BatchDetailServiceImpl(BatchDetailRepository batchDetailRepository, LogUtility logUtility,
			PaymentRequestRepository paymentRequestRepository) {
		this.batchDetailRepository = batchDetailRepository;
		this.logUtility = logUtility;
		this.paymentRequestRepository = paymentRequestRepository;
	}

	@Override
	public void saveBatchDetail(CorrelationDto correlationDto) {
		log.debug("batch detail event received");
		List<BatchDetail> existingBatchDetails = this.batchDetailRepository
			.findByCorrelationId(correlationDto.getCorrelationId());
		List<BatchDetail> batchDetailList = new ArrayList<>();
		correlationDto.getBatches().forEach((batchDetailDto) -> {
			batchDetailDto.getNetworks()
				.stream()
				.filter((networkDto) -> networkDto.getNetwork().equalsIgnoreCase(VIRTUALCARD))
				.findFirst()
				.ifPresent((networkDto) -> {
					if (this.batchDetailRepository.existsByCorrelationIdAndBatchId(correlationDto.getCorrelationId(),
							batchDetailDto.getBatchId())) {
						log.info("Skipping duplicate upstream batch for correlationId: {} and batchId: {}",
								correlationDto.getCorrelationId(), batchDetailDto.getBatchId());
						return;
					}

					if (hasSyntheticBatch(existingBatchDetails)) {
						if (!hasMatchingSyntheticBatch(existingBatchDetails, networkDto.getNumberOfPayments(),
								networkDto.getTotalAmount())) {
							log.warn(
									"Late upstream batch mismatch for correlationId: {}. eventCount: {}, eventAmount: {}",
									correlationDto.getCorrelationId(), networkDto.getNumberOfPayments(),
									networkDto.getTotalAmount());
						}
						log.info(
								"Skipping upstream batch insert because synthetic RRF batch already exists for correlationId: {}",
								correlationDto.getCorrelationId());
						return;
					}

					BatchDetail batchDetail = new BatchDetail();
					batchDetail.setOrg(correlationDto.getOrg());
					batchDetail.setCorrelationId(correlationDto.getCorrelationId());
					batchDetail.setBatchId(batchDetailDto.getBatchId());
					batchDetail.setNumberOfPayments(networkDto.getNumberOfPayments());
					batchDetail.setTotalAmount(networkDto.getTotalAmount());
					batchDetail.setStatus(BATCH_DETAIL_STATUS_INITIALIZE);
					batchDetail.setCreatedOn(LocalDateTime.now());
					batchDetailList.add(batchDetail);
				});
		});
		this.batchDetailRepository.saveAll(batchDetailList);
		log.info("batch details saved successfully");
	}

	private boolean hasSyntheticBatch(List<BatchDetail> existingBatchDetails) {
		return existingBatchDetails.stream()
			.anyMatch((detail) -> detail.getBatchId() != null
					&& detail.getBatchId().startsWith(RE_PREFERENCE_BATCH_ID_PREFIX));
	}

	private boolean hasMatchingSyntheticBatch(List<BatchDetail> existingBatchDetails, Long numberOfPayments,
			java.math.BigDecimal totalAmount) {
		return existingBatchDetails.stream()
			.filter((detail) -> detail.getBatchId() != null
					&& detail.getBatchId().startsWith(RE_PREFERENCE_BATCH_ID_PREFIX))
			.anyMatch((detail) -> Objects.equals(detail.getNumberOfPayments(), numberOfPayments)
					&& ((detail.getTotalAmount() == null && totalAmount == null) || (detail.getTotalAmount() != null
							&& totalAmount != null && detail.getTotalAmount().compareTo(totalAmount) == 0)));
	}

	@Override
	public ResponseEntity<Object> getBatchDetails(String correlationId) {
		List<BatchDetail> batchDetails = this.batchDetailRepository.findByCorrelationId(correlationId);
		if (batchDetails.isEmpty()) {
			return ResponseEntity.status(HttpStatus.NOT_FOUND)
				.body("No batch details found for the given correlation ID");
		}
		return ResponseEntity.ok(batchDetails);
	}

	@Override
	public List<CorrelatedBatchDetail> getCorrelatedBatch(String status) {
		return this.batchDetailRepository.getCorrelationRecordForProcessing(status);
	}

	@Override
	public void updateBatchDetailStatus(String correlationId, String status, String oldStatus) {
		this.batchDetailRepository.updateBatchDetailStatus(correlationId, status, oldStatus);
	}

	@Override
	public long timeDifferenceInHours(String correlationId) {
		BatchDetail batchDetail = this.batchDetailRepository.findByCorrelationIdLimit1(correlationId);
		return Duration
			.between((batchDetail.getCreatedOn() != null) ? batchDetail.getCreatedOn() : LocalDateTime.now(),
					LocalDateTime.now())
			.toHours();
	}

	@Override
	public List<BatchCorrelationDto> getBatchDetailsByFailStatus(String batchStatus, String enterDate) {
		List<BatchDetail> batchDetails = this.batchDetailRepository.findAllByStatus(batchStatus, enterDate);
		Map<String, List<BatchDetail>> batchDetailsMap = batchDetails.stream()
			.collect(Collectors.groupingBy(BatchDetail::getCorrelationId));

		List<BatchCorrelationDto> batchCorrelationDtoList = new ArrayList<>();
		batchDetailsMap.forEach((correlationId, batchDetailList) -> {
			BatchCorrelationDto batchCorrelationDto = new BatchCorrelationDto();
			batchCorrelationDto.setOrg(batchDetailList.getFirst().getOrg());
			batchCorrelationDto.setCorrelationId(correlationId);
			batchCorrelationDto.setNumberOfBatches(batchDetailList.size());
			List<BatchDto> batchDtoList = new ArrayList<>();
			batchDetailList.forEach((batchDetail) -> {
				BatchDto batchDto = new BatchDto();
				batchDto.setBatchId(batchDetail.getBatchId());
				batchDto.setNetwork(VIRTUALCARD);
				batchDto.setNumberOfPayments(batchDetail.getNumberOfPayments());
				batchDto.setTotalAmount(batchDetail.getTotalAmount());
				batchDto.setStatus(batchDetail.getStatus());
				batchDto.setCreatedOn(batchDetail.getCreatedOn());
				batchDto.setUpdatedOn(batchDetail.getUpdatedOn());
				batchDtoList.add(batchDto);
			});
			batchCorrelationDto.setBatchDto(batchDtoList);
			batchCorrelationDtoList.add(batchCorrelationDto);
		});
		return batchCorrelationDtoList;
	}

	@Override
	public PaymentRequestTotalDto getPaymentCountAndAmountByStatus(String correlationId) {
		this.logUtility.logMessage("Fetching payment count and amount for correlationId: " + correlationId, "INFO",
				log);
		List<PaymentRequestStatusAmountCountDto> paymentRequestStatusAmountCountDtoList = this.paymentRequestRepository
			.findPaymentStatsByCorrelationIdGroupByStatus(correlationId);

		Long totalPayments = 0L;
		BigDecimal totalAmount = BigDecimal.valueOf(0);
		for (PaymentRequestStatusAmountCountDto prDto : paymentRequestStatusAmountCountDtoList) {
			totalAmount = totalAmount.add(prDto.getTotalAmount());
			totalPayments += prDto.getNumberOfPayments();
		}
		PaymentRequestTotalDto paymentRequestTotalDto = new PaymentRequestTotalDto();
		paymentRequestTotalDto.setPaymentRequestStatusAmountCountDto(paymentRequestStatusAmountCountDtoList);
		paymentRequestTotalDto.setTotalPaymentCount(String.valueOf(totalPayments));
		paymentRequestTotalDto.setTotalAmount(String.valueOf(totalAmount));

		return paymentRequestTotalDto;
	}

	@Override
	@Transactional
	public int updateBatchDetailsByStatus(BatchDetailUpdateRequestDto request) {
		if (request.getNumberOfPayments() == null && request.getAmount() == null) {
			return this.batchDetailRepository.updateDetailsByCorrelationId(request.getStatus(),
					request.getCorrelationId(), request.getBatchId());
		}
		else if (request.getNumberOfPayments() == null) {
			return this.batchDetailRepository.updateDetailsByCorrelationId(request.getStatus(), request.getAmount(),
					request.getCorrelationId(), request.getBatchId());
		}
		else if (request.getAmount() == null) {
			return this.batchDetailRepository.updateDetailsByCorrelationId(request.getStatus(),
					request.getNumberOfPayments(), request.getCorrelationId(), request.getBatchId());
		}
		else {
			return this.batchDetailRepository.updateDetailsByCorrelationId(request.getStatus(), request.getAmount(),
					request.getNumberOfPayments(), request.getCorrelationId(), request.getBatchId());
		}
	}

	@Override
	@Transactional
	public int updateBatchesByStatus(BatchUpdateTimeRangeRequestDto request) {
		return this.batchDetailRepository.updateStatusByDateRange(request.getStatus(),
				request.getStartDate().atStartOfDay(), request.getEndDate().atStartOfDay());
	}

	public List<String> getCorrelationIdsList(int offset, int limit) {
		return this.batchDetailRepository.getCorrelationIds(offset, limit);
	}

}
