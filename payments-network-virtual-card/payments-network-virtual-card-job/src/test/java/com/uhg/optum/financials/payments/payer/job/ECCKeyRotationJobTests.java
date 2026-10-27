package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.batch.core.repository.JobRepository;
import org.springframework.transaction.PlatformTransactionManager;

import java.util.List;

@ExtendWith(MockitoExtension.class)
public class ECCKeyRotationJobTests {

	@Mock
	private JobRepository jobRepository;

	@Mock
	private PlatformTransactionManager transactionManager;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private ECCKeyRotationJob eccKeyRotationJob;

	@Test
	void testRun() throws Exception {
		// Arrange
		List<Throwable> exceptions = List.of(new RuntimeException("Test exception"));
		String jobId = this.eccKeyRotationJob.run();
		Assertions.assertThat(jobId).isNotNull();
	}

}
