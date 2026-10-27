package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackEnrollmentErrorDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackHeaderDto;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.util.List;
import java.util.Map;

@ExtendWith(MockitoExtension.class)
class StpEnrollmentMockCallbackControllerTests {

	@InjectMocks
	private StpEnrollmentMockCallbackController controller;

	@Mock
	private LogUtility logUtility;

	@Test
	void receiveMockCallback_returnsSuccessPayload() {
		StpEnrollmentCallbackHeaderDto header = new StpEnrollmentCallbackHeaderDto();
		header.setCallbackUrl("http://localhost:8083/api/mock/virtual-card/cards/stp-enrollment-callback");
		header.setCorrelationId("STP-CORR-001");
		header.setNetworkPartner("STP Partner");
		header.setStatus("Error");

		StpEnrollmentCallbackEnrollmentErrorDto enrollmentError = new StpEnrollmentCallbackEnrollmentErrorDto();
		enrollmentError.setProviderTin("123456789");
		enrollmentError.setNpi("1234567890");
		enrollmentError.setEnrollmentType("B2B");
		enrollmentError.setErrors(List.of("Invalid Zip Code"));

		StpEnrollmentCallbackDto callbackDto = new StpEnrollmentCallbackDto();
		callbackDto.setHeader(header);
		callbackDto.setEnrollmentErrors(List.of(enrollmentError));

		ResponseEntity<Map<String, Object>> response = this.controller.receiveMockCallback(callbackDto);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isNotNull();
		Assertions.assertThat(response.getBody().get("status")).isEqualTo("success");
		Assertions.assertThat(response.getBody().get("receivedErrors")).isEqualTo(1);
		Assertions.assertThat(response.getBody().get("correlationId")).isEqualTo("STP-CORR-001");
		Assertions.assertThat(response.getBody().get("errorSummary")).asList().hasSize(1);
		Map<String, Object> errorSummary = ((List<Map<String, Object>>) response.getBody().get("errorSummary"))
			.getFirst();
		Assertions.assertThat(errorSummary.get("providerTin")).isEqualTo("123456789");
		Assertions.assertThat(errorSummary.get("npi")).isEqualTo("1234567890");
		Assertions.assertThat(errorSummary.get("enrollmentType")).isEqualTo("B2B");
		Assertions.assertThat(errorSummary.get("errorCount")).isEqualTo(1);
		Assertions.assertThat(errorSummary.get("errors")).asList().containsExactly("Invalid Zip Code");
	}

}
