package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.PaymentRequestStatusAmountCountDto;
import com.uhg.optum.financials.payments.payer.dto.PaymentRequestTotalDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchCorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchDetailDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchDetailUpdateRequestDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchNetworkDto;
import com.uhg.optum.financials.payments.payer.dto.batch.BatchUpdateTimeRangeRequestDto;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelatedBatchDetail;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.model.BatchDetail;
import com.uhg.optum.financials.payments.payer.repository.BatchDetailRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUALCARD;
import static org.assertj.core.api.AssertionsForClassTypes.assertThat;
import static org.assertj.core.api.AssertionsForClassTypes.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;

class BatchDetailServiceImplTests {

	@Mock
	private BatchDetailRepository batchDetailRepository;

	@Mock
	private LogUtility logUtility;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@InjectMocks
	private BatchDetailServiceImpl batchDetailServiceImpl;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void saveBatchDetail_savesBatchDetailsWhenNetworkIsOptumPay() {
		CorrelationDto correlationDto = new CorrelationDto();
		correlationDto.setOrg("Org1");
		correlationDto.setCorrelationId("CorrId1");
		correlationDto.setBatches(Collections.singletonList(new BatchDetailDto("1",
				Collections.singletonList(new BatchNetworkDto("OPTUMPAY", 10L, BigDecimal.valueOf(100.0))))));
		this.batchDetailServiceImpl.saveBatchDetail(correlationDto);
		verify(this.batchDetailRepository, times(1)).saveAll(anyList());
	}

	@Test
	void saveBatchDetail_savesMultipleBatchDetails() {
		CorrelationDto correlationDto = new CorrelationDto();
		correlationDto.setOrg("Org1");
		correlationDto.setCorrelationId("CorrId1");
		correlationDto.setBatches(Arrays.asList(
				new BatchDetailDto("1",
						Arrays.asList(new BatchNetworkDto(VIRTUALCARD, 10L, BigDecimal.valueOf(100.0)),
								new BatchNetworkDto(VIRTUALCARD, 20L, BigDecimal.valueOf(200.0)))),
				new BatchDetailDto("2", Arrays.asList(new BatchNetworkDto(VIRTUALCARD, 30L, BigDecimal.valueOf(100.0)),
						new BatchNetworkDto("ECHO", 20L, BigDecimal.valueOf(200.0))))));
		this.batchDetailServiceImpl.saveBatchDetail(correlationDto);
		verify(this.batchDetailRepository, times(1)).saveAll(anyList());
	}

	@Test
	void getBatchDetails_returnsBatchDetails() {
		BatchDetail batchDetail = new BatchDetail();
		batchDetail.setCorrelationId("CorrId1");
		Mockito.when(this.batchDetailRepository.findByCorrelationId("CorrId1")).thenReturn(Arrays.asList(batchDetail));
		ResponseEntity<Object> result = this.batchDetailServiceImpl.getBatchDetails("CorrId1");
		assertThat(result.getBody()).isInstanceOf(List.class);
	}

	@Test
	void getBatchDetails_returnsEmptyListWhenNoBatchDetailsFound() {
		Mockito.when(this.batchDetailRepository.findByCorrelationId("CorrId1")).thenReturn(Arrays.asList());
		ResponseEntity<Object> result = this.batchDetailServiceImpl.getBatchDetails("CorrId1");
		assertThat(result.getStatusCode()).isEqualTo(HttpStatus.NOT_FOUND);
	}

	@Test
	void getCorrelationRecordForProcessing_returnsCorrelationRecords() {
		List<CorrelatedBatchDetail> correlatedBatchDetailList = Collections.singletonList(new CorrelatedBatchDetail() {
			@Override
			public Long getNumberOfPayments() {
				return 10L;
			}

			@Override
			public String getCorrelationId() {
				return "CorrId1";
			}

			@Override
			public BigDecimal getTotalAmount() {
				return BigDecimal.valueOf(100.00);
			}
		});
		Mockito.when(this.batchDetailRepository.getCorrelationRecordForProcessing("INITIALIZE"))
			.thenReturn(correlatedBatchDetailList);
		List<CorrelatedBatchDetail> result = this.batchDetailServiceImpl.getCorrelatedBatch("INITIALIZE");
		assertThat(result).isNotNull();
		assertThat(result.getFirst().getCorrelationId()).isEqualTo("CorrId1");
	}

	@Test
	void getCorrelatedBatch_returnsEmptyListWhenNoRecordsFound() {
		Mockito.when(this.batchDetailRepository.getCorrelationRecordForProcessing("INITIALIZE")).thenReturn(List.of());
		List<CorrelatedBatchDetail> result = this.batchDetailServiceImpl.getCorrelatedBatch("INITIALIZE");
		assertThat(result).isEqualTo(Collections.emptyList());
	}

	@Test
	void updateBatchDetailStatus_updatesStatus() {
		Mockito.doNothing()
			.when(this.batchDetailRepository)
			.updateBatchDetailStatus("CorrId1", "PROCESSED", "INITIALIZE");
		this.batchDetailServiceImpl.updateBatchDetailStatus("CorrId1", "PROCESSED", "INITIALIZE");
		verify(this.batchDetailRepository, times(1)).updateBatchDetailStatus("CorrId1", "PROCESSED", "INITIALIZE");
	}

	@Test
	void timeDifferenceInHours_returnsCorrectDifferenceForValidCorrelationId() {
		BatchDetail batchDetail = new BatchDetail();
		batchDetail.setCreatedOn(LocalDateTime.now().minusHours(5));
		Mockito.when(this.batchDetailRepository.findByCorrelationIdLimit1("CorrId1")).thenReturn(batchDetail);

		long result = this.batchDetailServiceImpl.timeDifferenceInHours("CorrId1");

		assertThat(result).isEqualTo(5);
	}

	@Test
	void timeDifferenceInHours_returnsZeroWhenCreatedOnIsNow() {
		BatchDetail batchDetail = new BatchDetail();
		batchDetail.setCreatedOn(LocalDateTime.now());
		Mockito.when(this.batchDetailRepository.findByCorrelationIdLimit1("CorrId1")).thenReturn(batchDetail);

		long result = this.batchDetailServiceImpl.timeDifferenceInHours("CorrId1");

		assertThat(result).isZero();
	}

	@Test
	void timeDifferenceInHours_throwsExceptionWhenCorrelationIdNotFound() {
		Mockito.when(this.batchDetailRepository.findByCorrelationIdLimit1("InvalidCorrId")).thenReturn(null);

		assertThatThrownBy(() -> this.batchDetailServiceImpl.timeDifferenceInHours("InvalidCorrId"))
			.isInstanceOf(NullPointerException.class);
	}

	@Test
	void getBatchDetailsByFailStatus_returnsBatchCorrelationDtosForValidStatus() {
		BatchDetail batchDetail1 = new BatchDetail();
		batchDetail1.setOrg("Org1");
		batchDetail1.setCorrelationId("CorrId1");
		batchDetail1.setBatchId("Batch1");
		batchDetail1.setNumberOfPayments(10L);
		batchDetail1.setTotalAmount(BigDecimal.valueOf(100.0));
		batchDetail1.setStatus("FAILED");
		batchDetail1.setCreatedOn(LocalDateTime.now());
		batchDetail1.setUpdatedOn(LocalDateTime.now());

		BatchDetail batchDetail2 = new BatchDetail();
		batchDetail2.setOrg("Org1");
		batchDetail2.setCorrelationId("CorrId1");
		batchDetail2.setBatchId("Batch2");
		batchDetail2.setNumberOfPayments(20L);
		batchDetail2.setTotalAmount(BigDecimal.valueOf(200.0));
		batchDetail2.setStatus("FAILED");
		batchDetail2.setCreatedOn(LocalDateTime.now());
		batchDetail2.setUpdatedOn(LocalDateTime.now());

		Mockito.when(this.batchDetailRepository.findAllByStatus("FAILED", "2023-10-01"))
			.thenReturn(Arrays.asList(batchDetail1, batchDetail2));

		List<BatchCorrelationDto> result = this.batchDetailServiceImpl.getBatchDetailsByFailStatus("FAILED",
				"2023-10-01");

		assertThat(result).isNotNull();
		assertThat(1).isEqualTo(result.size());
		assertThat(result.getFirst().getCorrelationId()).isEqualTo("CorrId1");
		assertThat(result.getFirst().getNumberOfBatches()).isEqualTo(2);
		assertThat(2).isEqualTo(result.getFirst().getBatchDto().size());
		assertThat(result.getFirst().getBatchDto().get(0).getBatchId()).isEqualTo("Batch1");
		assertThat(result.getFirst().getBatchDto().get(1).getBatchId()).isEqualTo("Batch2");
	}

	@Test
	void getBatchDetailsByFailStatus_returnsEmptyListWhenNoBatchesFound() {
		Mockito.when(this.batchDetailRepository.findAllByStatus("FAILED", "2023-10-01"))
			.thenReturn(Collections.emptyList());
		List<BatchCorrelationDto> result = this.batchDetailServiceImpl.getBatchDetailsByFailStatus("FAILED",
				"2023-10-01");
		assertThat(result).isNotNull();
	}

	@ParameterizedTest
	@ValueSource(strings = { "CORR123", "CORR456" })
	void getPaymentCountAndAmount_returnsPaymentRequestAmountCountDtoForValidInputs(String correlationId) {
		PaymentRequestTotalDto dto = new PaymentRequestTotalDto();
		dto.setTotalPaymentCount("10");
		dto.setTotalAmount("2000.00");
		dto.setPaymentRequestStatusAmountCountDto(
				List.of(new PaymentRequestStatusAmountCountDto("FT", 5L, new BigDecimal("1000.00")),
						new PaymentRequestStatusAmountCountDto("PI", 3L, new BigDecimal("2000.00"))));

		Mockito.when(this.paymentRequestRepository.findPaymentStatsByCorrelationIdGroupByStatus(correlationId))
			.thenReturn(dto.getPaymentRequestStatusAmountCountDto());
		/*
		 * Mockito.when(this.paymentRequestRepository.findTotalPaymentCount(correlationId)
		 * ) .thenReturn(10,23);
		 */

		// PaymentRequestTotalDto result =
		// this.batchDetailServiceImpl.getPaymentCountAndAmountByStatus(correlationId);

		// assertThat(result).isNotNull();
		// assertThat(result.getTotalPaymentCount()).isEqualTo("10");
	}

	@Test
	void getPaymentCountAndAmountByStatus_returnsPaymentRequestTotalDto() {
		List<PaymentRequestStatusAmountCountDto> dtoList = List.of(
				new PaymentRequestStatusAmountCountDto("COMPLETE", 5L, new BigDecimal("1000.00")),
				new PaymentRequestStatusAmountCountDto("PENDING", 3L, new BigDecimal("500.00")));

		Mockito.when(this.paymentRequestRepository.findPaymentStatsByCorrelationIdGroupByStatus("CorrId1"))
			.thenReturn(dtoList);

		PaymentRequestTotalDto result = this.batchDetailServiceImpl.getPaymentCountAndAmountByStatus("CorrId1");

		assertThat(result).isNotNull();
		assertThat(result.getPaymentRequestStatusAmountCountDto()).isEqualTo(dtoList);
		assertThat(result.getTotalPaymentCount()).isEqualTo("8");
		assertThat(result.getTotalAmount()).isEqualTo("1500.00");
	}

	@Test
	void getPaymentCountAndAmountByStatus_returnsEmptyTotalsWhenNoStatusFound() {
		Mockito.when(this.paymentRequestRepository.findPaymentStatsByCorrelationIdGroupByStatus("CorrId1"))
			.thenReturn(Collections.emptyList());

		PaymentRequestTotalDto result = this.batchDetailServiceImpl.getPaymentCountAndAmountByStatus("CorrId1");

		assertThat(result).isNotNull();
		assertThat(result.getTotalPaymentCount()).isEqualTo("0");
		assertThat(result.getTotalAmount()).isEqualTo("0");
	}

	@Test
	void updateBatchDetailsByStatus_updatesWithOnlyStatus_whenNoAmountOrPaymentCount() {
		BatchDetailUpdateRequestDto request = new BatchDetailUpdateRequestDto();
		request.setStatus("PROCESSED");
		request.setCorrelationId("CorrId1");
		request.setBatchId("Batch1");

		Mockito.when(this.batchDetailRepository.updateDetailsByCorrelationId("PROCESSED", "CorrId1", "Batch1"))
			.thenReturn(1);

		int result = this.batchDetailServiceImpl.updateBatchDetailsByStatus(request);

		assertThat(result).isEqualTo(1);
		verify(this.batchDetailRepository, times(1)).updateDetailsByCorrelationId("PROCESSED", "CorrId1", "Batch1");
	}

	@Test
	void updateBatchDetailsByStatus_updatesWithStatusAndAmount_whenNoPaymentCount() {
		BatchDetailUpdateRequestDto request = new BatchDetailUpdateRequestDto();
		request.setStatus("PROCESSED");
		request.setCorrelationId("CorrId1");
		request.setBatchId("Batch1");
		request.setAmount(BigDecimal.valueOf(500.0));

		Mockito
			.when(this.batchDetailRepository.updateDetailsByCorrelationId("PROCESSED", BigDecimal.valueOf(500.0),
					"CorrId1", "Batch1"))
			.thenReturn(1);

		int result = this.batchDetailServiceImpl.updateBatchDetailsByStatus(request);

		assertThat(result).isEqualTo(1);
		verify(this.batchDetailRepository, times(1)).updateDetailsByCorrelationId("PROCESSED",
				BigDecimal.valueOf(500.0), "CorrId1", "Batch1");
	}

	@Test
	void updateBatchDetailsByStatus_updatesWithStatusAndPaymentCount_whenNoAmount() {
		BatchDetailUpdateRequestDto request = new BatchDetailUpdateRequestDto();
		request.setStatus("PROCESSED");
		request.setCorrelationId("CorrId1");
		request.setBatchId("Batch1");
		request.setNumberOfPayments(15L);

		Mockito.when(this.batchDetailRepository.updateDetailsByCorrelationId("PROCESSED", 15L, "CorrId1", "Batch1"))
			.thenReturn(1);

		int result = this.batchDetailServiceImpl.updateBatchDetailsByStatus(request);

		assertThat(result).isEqualTo(1);
		verify(this.batchDetailRepository, times(1)).updateDetailsByCorrelationId("PROCESSED", 15L, "CorrId1",
				"Batch1");
	}

	@Test
	void updateBatchDetailsByStatus_updatesWithAllFields_whenAllFieldsProvided() {
		BatchDetailUpdateRequestDto request = new BatchDetailUpdateRequestDto();
		request.setStatus("PROCESSED");
		request.setCorrelationId("CorrId1");
		request.setBatchId("Batch1");
		request.setNumberOfPayments(15L);
		request.setAmount(BigDecimal.valueOf(500.0));

		Mockito
			.when(this.batchDetailRepository.updateDetailsByCorrelationId("PROCESSED", BigDecimal.valueOf(500.0), 15L,
					"CorrId1", "Batch1"))
			.thenReturn(1);

		int result = this.batchDetailServiceImpl.updateBatchDetailsByStatus(request);

		assertThat(result).isEqualTo(1);
		verify(this.batchDetailRepository, times(1)).updateDetailsByCorrelationId("PROCESSED",
				BigDecimal.valueOf(500.0), 15L, "CorrId1", "Batch1");
	}

	@Test
	void updateBatchesByStatus_updatesStatusForDateRange() {
		LocalDate startDate = LocalDate.of(2023, 10, 1);
		LocalDate endDate = LocalDate.of(2023, 10, 31);

		BatchUpdateTimeRangeRequestDto request = new BatchUpdateTimeRangeRequestDto();
		request.setStatus("PROCESSED");
		request.setStartDate(startDate);
		request.setEndDate(endDate);

		Mockito
			.when(this.batchDetailRepository.updateStatusByDateRange("PROCESSED", startDate.atStartOfDay(),
					endDate.atStartOfDay()))
			.thenReturn(5);

		int result = this.batchDetailServiceImpl.updateBatchesByStatus(request);

		assertThat(result).isEqualTo(5);
		verify(this.batchDetailRepository, times(1)).updateStatusByDateRange("PROCESSED", startDate.atStartOfDay(),
				endDate.atStartOfDay());
	}

	@Test
	void getCorrelationIdsList_returnsListOfCorrelationIds() {
		List<String> correlationIds = Arrays.asList("CorrId1", "CorrId2", "CorrId3");
		Mockito.when(this.batchDetailRepository.getCorrelationIds(0, 10)).thenReturn(correlationIds);

		List<String> result = this.batchDetailServiceImpl.getCorrelationIdsList(0, 10);

		assertThat(result).isEqualTo(correlationIds);
		verify(this.batchDetailRepository, times(1)).getCorrelationIds(0, 10);
	}

}
