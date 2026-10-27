package com.uhg.optum.financials.payments.payer.job.service;

import com.uhg.optum.financials.payments.payer.dto.RePreferencePaymentRow;
import com.uhg.optum.financials.payments.payer.model.BatchDetail;
import com.uhg.optum.financials.payments.payer.model.CorrelationMapping;
import com.uhg.optum.financials.payments.payer.repository.BatchDetailRepository;
import com.uhg.optum.financials.payments.payer.repository.CorrelationMappingRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.math.BigDecimal;
import java.util.Collections;
import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_INITIALIZE;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.RE_PREFERENCE_MAX_ATTEMPTS;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.BDDMockito.given;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class RePreferenceCorrelationServiceTests {

	@InjectMocks
	private RePreferenceCorrelationService service;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private BatchDetailRepository batchDetailRepository;

	@Mock
	private CorrelationMappingRepository correlationMappingRepository;

	@Test
	void processGroup_doesNothing_whenNoPaymentRowsFound() {
		given(this.paymentRequestRepository.findRePreferencePayments("ORG1", "CORR1", RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(Collections.emptyList());

		this.service.processGroup("ORG1", "CORR1");

		verify(this.correlationMappingRepository, never()).save(any());
		verify(this.batchDetailRepository, never()).saveAndFlush(any());
		verify(this.paymentRequestRepository, never()).updateCorrelationAndMarkRePreferenceProcessed(anyList(),
				anyString(), anyString());
	}

	@Test
	void processGroup_createsCorrelationMappingAndBatchDetail_whenRowsFound() {
		RePreferencePaymentRow row1 = paymentRow(10L, new BigDecimal("500.00"));
		RePreferencePaymentRow row2 = paymentRow(11L, new BigDecimal("250.50"));
		given(this.paymentRequestRepository.findRePreferencePayments("ORG1", "CORR1", RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(List.of(row1, row2));
		given(this.correlationMappingRepository.existsById(anyString())).willReturn(false);
		given(this.batchDetailRepository.existsByCorrelationIdAndBatchId(anyString(), anyString())).willReturn(false);
		given(this.paymentRequestRepository.updateCorrelationAndMarkRePreferenceProcessed(anyList(), anyString(),
				anyString()))
			.willReturn(2);

		this.service.processGroup("ORG1", "CORR1");

		ArgumentCaptor<CorrelationMapping> mappingCaptor = ArgumentCaptor.forClass(CorrelationMapping.class);
		verify(this.correlationMappingRepository).save(mappingCaptor.capture());
		CorrelationMapping savedMapping = mappingCaptor.getValue();
		assertThat(savedMapping.getActualCorrelationId()).isEqualTo("CORR1");
		assertThat(savedMapping.getInternalCorrelationId()).startsWith("RRI");

		ArgumentCaptor<BatchDetail> batchCaptor = ArgumentCaptor.forClass(BatchDetail.class);
		verify(this.batchDetailRepository).saveAndFlush(batchCaptor.capture());
		BatchDetail savedBatch = batchCaptor.getValue();
		assertThat(savedBatch.getOrg()).isEqualTo("ORG1");
		assertThat(savedBatch.getCorrelationId()).startsWith("RRI");
		assertThat(savedBatch.getNumberOfPayments()).isEqualTo(2L);
		assertThat(savedBatch.getTotalAmount()).isEqualByComparingTo(new BigDecimal("750.50"));
		assertThat(savedBatch.getStatus()).isEqualTo(BATCH_DETAIL_STATUS_INITIALIZE);
		assertThat(savedBatch.getBatchId()).startsWith("RRF");
	}

	@Test
	void processGroup_treatsNullAmountAsZero_whenRowAmountIsNull() {
		RePreferencePaymentRow rowWithNullAmount = paymentRow(20L, null);
		RePreferencePaymentRow rowWithAmount = paymentRow(21L, new BigDecimal("300.00"));
		given(this.paymentRequestRepository.findRePreferencePayments("ORG1", "CORR1", RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(List.of(rowWithNullAmount, rowWithAmount));
		given(this.correlationMappingRepository.existsById(anyString())).willReturn(false);
		given(this.batchDetailRepository.existsByCorrelationIdAndBatchId(anyString(), anyString())).willReturn(false);
		given(this.paymentRequestRepository.updateCorrelationAndMarkRePreferenceProcessed(anyList(), anyString(),
				anyString()))
			.willReturn(2);

		this.service.processGroup("ORG1", "CORR1");

		ArgumentCaptor<BatchDetail> captor = ArgumentCaptor.forClass(BatchDetail.class);
		verify(this.batchDetailRepository).saveAndFlush(captor.capture());
		assertThat(captor.getValue().getTotalAmount()).isEqualByComparingTo(new BigDecimal("300.00"));
	}

	@Test
	void processGroup_throwsIllegalStateException_whenUpdatedCountMismatchesLockedCount() {
		RePreferencePaymentRow row = paymentRow(30L, new BigDecimal("100.00"));
		given(this.paymentRequestRepository.findRePreferencePayments("ORG1", "CORR1", RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(List.of(row));
		given(this.correlationMappingRepository.existsById(anyString())).willReturn(false);
		given(this.paymentRequestRepository.updateCorrelationAndMarkRePreferenceProcessed(anyList(), anyString(),
				anyString()))
			.willReturn(0);

		assertThatThrownBy(() -> this.service.processGroup("ORG1", "CORR1")).isInstanceOf(IllegalStateException.class)
			.hasMessageContaining("CORR1");
	}

	@Test
	void processGroup_passesCorrectPaymentIdsToUpdate() {
		RePreferencePaymentRow row1 = paymentRow(40L, BigDecimal.TEN);
		RePreferencePaymentRow row2 = paymentRow(41L, BigDecimal.ONE);
		given(this.paymentRequestRepository.findRePreferencePayments("ORG1", "CORR1", RE_PREFERENCE_MAX_ATTEMPTS))
			.willReturn(List.of(row1, row2));
		given(this.correlationMappingRepository.existsById(anyString())).willReturn(false);
		given(this.batchDetailRepository.existsByCorrelationIdAndBatchId(anyString(), anyString())).willReturn(false);
		given(this.paymentRequestRepository.updateCorrelationAndMarkRePreferenceProcessed(anyList(), anyString(),
				anyString()))
			.willReturn(2);

		this.service.processGroup("ORG1", "CORR1");

		@SuppressWarnings("unchecked")
		ArgumentCaptor<List<Long>> idCaptor = ArgumentCaptor.forClass(List.class);
		verify(this.paymentRequestRepository).updateCorrelationAndMarkRePreferenceProcessed(idCaptor.capture(),
				anyString(), anyString());
		assertThat(idCaptor.getValue()).containsExactlyInAnyOrder(40L, 41L);
	}

	private static RePreferencePaymentRow paymentRow(Long id, BigDecimal amount) {
		return new RePreferencePaymentRow() {
			@Override
			public Long getId() {
				return id;
			}

			@Override
			public BigDecimal getAmount() {
				return amount;
			}
		};
	}

}
