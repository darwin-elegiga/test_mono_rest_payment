package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.repository.ProcessedWebhookRepository;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class DuplicateWebhookCheckUtilTests {

	@InjectMocks
	DuplicateWebhookCheckUtil duplicateWebhookCheckUtil;

	@Mock
	ProcessedWebhookRepository processedWebhookRepository;

	@Test
	void checkForProcessedWebhook_returnsZero_whenAlreadyProcessed() {
		String stage = "RECEIVED";
		BDDMockito.given(this.processedWebhookRepository.tryInsertProcessed("id1", "type1", stage)).willReturn(0);

		int result = this.duplicateWebhookCheckUtil.checkForProcessedWebhook("id1", "type1", stage);

		Assertions.assertThat(result).isZero();
		BDDMockito.then(this.processedWebhookRepository).should().tryInsertProcessed("id1", "type1", stage);
	}

	@Test
	void checkForProcessedWebhook_returnsOne_whenNotProcessed() {
		String stage = "RECEIVED";
		BDDMockito.given(this.processedWebhookRepository.tryInsertProcessed("id2", "type2", stage)).willReturn(1);

		int result = this.duplicateWebhookCheckUtil.checkForProcessedWebhook("id2", "type2", stage);

		Assertions.assertThat(result).isEqualTo(1);
		BDDMockito.then(this.processedWebhookRepository).should().tryInsertProcessed("id2", "type2", stage);
	}

}
