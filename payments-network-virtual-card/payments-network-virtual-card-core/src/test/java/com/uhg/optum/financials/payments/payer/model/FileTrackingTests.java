package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.payments.payer.dto.FileTrackingDto;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.mockito.BDDMockito.then;

@ExtendWith(MockitoExtension.class)
public class FileTrackingTests {

	@InjectMocks
	FileTracking fileTracking;

	@Mock
	FileTrackingDto fileTrackingDto;

	@Test
	void testToDto() {
		this.fileTracking.setCorrelationId("123");
		this.fileTracking.setCompanion("testCompanion");
		this.fileTracking.setCompanionStatus("SUCCESS");
		this.fileTracking.setPaymentActivityStatus("SUCCESS");
		Assertions.assertThat(this.fileTracking.toDto(this.fileTrackingDto)).isNotNull();
		then(this.fileTrackingDto).should().setCompanionStatus("SUCCESS");
		then(this.fileTrackingDto).should().setPaymentActivityStatus("SUCCESS");
	}

}
