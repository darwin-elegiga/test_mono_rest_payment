package com.uhg.optum.financials.payments.payer.job.task;

import com.uhg.optum.financials.payments.payer.enums.Status;
import com.uhg.optum.financials.payments.payer.enums.VirtualCardState;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.batch.core.scope.context.ChunkContext;
import org.springframework.batch.core.step.StepContribution;
import org.springframework.batch.infrastructure.repeat.RepeatStatus;

import java.util.ArrayList;
import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.BATCH_DETAIL_STATUS_835_CREATED;

@ExtendWith(MockitoExtension.class)
class CompanionFileTaskTests {

	private CompanionFileTask companionFileTask;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private StepContribution stepContribution;

	@Mock
	private ChunkContext chunkContext;

	private List<String> eligibleCorrelationIds;

	@BeforeEach
	void setUp() {
		this.eligibleCorrelationIds = new ArrayList<>();
		this.companionFileTask = new CompanionFileTask(this.paymentRequestRepository, this.eligibleCorrelationIds);
	}

	@Test
	void testExecuteWithNoEligibleCorrelationIds() throws Exception {
		Mockito
			.when(this.paymentRequestRepository.findEligibleCompanionCorrelationIds(BATCH_DETAIL_STATUS_835_CREATED,
					Status.PC.name(), VirtualCardState.CREATED.name()))
			.thenReturn(List.of());

		RepeatStatus result = this.companionFileTask.execute(this.stepContribution, this.chunkContext);

		Assertions.assertThat(result).isEqualTo(RepeatStatus.FINISHED);
		Assertions.assertThat(this.eligibleCorrelationIds).isEmpty();
		Mockito.verify(this.paymentRequestRepository)
			.findEligibleCompanionCorrelationIds(BATCH_DETAIL_STATUS_835_CREATED, Status.PC.name(),
					VirtualCardState.CREATED.name());
		Mockito.verifyNoMoreInteractions(this.paymentRequestRepository);
	}

	@Test
	void testExecuteWithSingleEligibleCorrelationId() throws Exception {
		Mockito
			.when(this.paymentRequestRepository.findEligibleCompanionCorrelationIds(BATCH_DETAIL_STATUS_835_CREATED,
					Status.PC.name(), VirtualCardState.CREATED.name()))
			.thenReturn(List.of("corr-007"));

		RepeatStatus result = this.companionFileTask.execute(this.stepContribution, this.chunkContext);

		Assertions.assertThat(result).isEqualTo(RepeatStatus.FINISHED);
		Assertions.assertThat(this.eligibleCorrelationIds).containsExactly("corr-007");
		Mockito.verify(this.paymentRequestRepository)
			.findEligibleCompanionCorrelationIds(BATCH_DETAIL_STATUS_835_CREATED, Status.PC.name(),
					VirtualCardState.CREATED.name());
	}

	@Test
	void testExecuteWithMultipleEligibleCorrelationIds() throws Exception {
		Mockito
			.when(this.paymentRequestRepository.findEligibleCompanionCorrelationIds(BATCH_DETAIL_STATUS_835_CREATED,
					Status.PC.name(), VirtualCardState.CREATED.name()))
			.thenReturn(List.of("corr-101", "corr-102", "corr-103"));

		RepeatStatus result = this.companionFileTask.execute(this.stepContribution, this.chunkContext);

		Assertions.assertThat(result).isEqualTo(RepeatStatus.FINISHED);
		Assertions.assertThat(this.eligibleCorrelationIds)
			.containsExactlyInAnyOrder("corr-101", "corr-102", "corr-103");
		Mockito.verify(this.paymentRequestRepository)
			.findEligibleCompanionCorrelationIds(BATCH_DETAIL_STATUS_835_CREATED, Status.PC.name(),
					VirtualCardState.CREATED.name());
	}

	@Test
	void testExecutePropagatesRepositoryException() {
		RuntimeException exception = new RuntimeException("Database error");
		Mockito
			.when(this.paymentRequestRepository.findEligibleCompanionCorrelationIds(BATCH_DETAIL_STATUS_835_CREATED,
					Status.PC.name(), VirtualCardState.CREATED.name()))
			.thenThrow(exception);

		Assertions.assertThatThrownBy(() -> this.companionFileTask.execute(this.stepContribution, this.chunkContext))
			.isInstanceOf(RuntimeException.class)
			.hasMessageContaining("Database error");
	}

	@Test
	void testExecuteReplacesEligibleCorrelationIdsFromPreviousRun() throws Exception {
		this.eligibleCorrelationIds.add("existing-corr");
		Mockito
			.when(this.paymentRequestRepository.findEligibleCompanionCorrelationIds(BATCH_DETAIL_STATUS_835_CREATED,
					Status.PC.name(), VirtualCardState.CREATED.name()))
			.thenReturn(List.of("new-corr"));

		RepeatStatus result = this.companionFileTask.execute(this.stepContribution, this.chunkContext);

		Assertions.assertThat(result).isEqualTo(RepeatStatus.FINISHED);
		Assertions.assertThat(this.eligibleCorrelationIds).containsExactly("new-corr");
		Mockito.verify(this.paymentRequestRepository)
			.findEligibleCompanionCorrelationIds(BATCH_DETAIL_STATUS_835_CREATED, Status.PC.name(),
					VirtualCardState.CREATED.name());
	}

}
