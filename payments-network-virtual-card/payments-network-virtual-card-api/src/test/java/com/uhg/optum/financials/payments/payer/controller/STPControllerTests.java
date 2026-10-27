package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentHeaderDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentRecordDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpPartnerProviderDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentRequestDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentResponseDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpRejectReasonDto;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.producer.StorageQueueProducerApi;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.test.util.ReflectionTestUtils;

import java.util.List;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.FAILED;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.SUCCESS;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;

@ExtendWith(MockitoExtension.class)
class STPControllerTests {

	@InjectMocks
	private STPController stpController;

	@Mock
	private StorageQueueProducerApi storageQueueProducerApi;

	@Mock
	private LogUtility logUtility;

	@Test
	void receiveProviderEnrollment_returnsCreatedAndAcknowledgement() {
		ReflectionTestUtils.setField(this.stpController, "callbackUrl", "https://someendpoint.net/enrollment/callback");
		given(this.storageQueueProducerApi.sendMessageToQueue(any(StpProviderEnrollmentRequestDto.class)))
			.willReturn(SUCCESS);

		ResponseEntity<StpProviderEnrollmentResponseDto> response = this.stpController
			.receiveProviderEnrollment(buildRequest());

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.ACCEPTED);
		Assertions.assertThat(response.getBody()).isNotNull();
		Assertions.assertThat(response.getBody().getHeader().getStatus()).isEqualTo("Success");
		Assertions.assertThat(response.getBody().getHeader().getCallbackUrl())
			.isEqualTo("https://someendpoint.net/enrollment/callback");
		Assertions.assertThat(response.getBody().getHeader().getCorrelationId()).isEqualTo("287678765456");
		Assertions.assertThat(response.getBody().getHeader().getNetworkPartner()).isEqualTo("STP Partner");
	}

	@Test
	void receiveProviderEnrollment_whenQueuePublishFails_throwsInternalServerException() {
		given(this.storageQueueProducerApi.sendMessageToQueue(any(StpProviderEnrollmentRequestDto.class)))
			.willReturn(FAILED);

		Assertions.assertThatThrownBy(() -> this.stpController.receiveProviderEnrollment(buildRequest()))
			.isInstanceOf(InternalServerException.class)
			.hasMessageContaining("Failed to enqueue STP provider enrollment request");
	}

	@Test
	void stpPaymentRejectReason_returnsAcceptedWhenQueued() {
		given(this.storageQueueProducerApi.sendMessageToQueue(any(StpRejectReasonDto.class))).willReturn(SUCCESS);

		ResponseEntity<String> response = this.stpController.stpPaymentRejectReason(buildRejectRequest());

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.ACCEPTED);
		Assertions.assertThat(response.getBody()).isEqualTo("Success");
	}

	@Test
	void stpPaymentRejectReason_whenQueuePublishFails_throwsInternalServerException() {
		given(this.storageQueueProducerApi.sendMessageToQueue(any(StpRejectReasonDto.class))).willReturn(FAILED);

		Assertions.assertThatThrownBy(() -> this.stpController.stpPaymentRejectReason(buildRejectRequest()))
			.isInstanceOf(InternalServerException.class)
			.hasMessageContaining("Failed to enqueue STP payment rejection request");
	}

	private StpProviderEnrollmentRequestDto buildRequest() {
		StpEnrollmentHeaderDto header = new StpEnrollmentHeaderDto();
		header.setNetworkPartner("STP Partner");
		header.setCorrelationId("287678765456");
		header.setCount(1);

		StpEnrollmentRecordDto enrollment = new StpEnrollmentRecordDto();
		enrollment.setNpi("1234567890");
		enrollment.setUuid("1234567890986757467783");
		enrollment.setAddress1("123 Test Lane");
		enrollment.setCity("Eden Prairie");
		enrollment.setState("MN");
		enrollment.setZip9("55435");
		enrollment.setEnrollmentType("C2B");
		enrollment.setEnrollmentEligibilityStatus("Y");
		enrollment.setEffectiveDate("2023-09-24");
		enrollment.setInitialEnrollmentDate("2023-09-24");
		enrollment.setLastUpdatedUTCDate("2023-10-24T23:33:00Z");
		enrollment.setEnrollmentStatusCd("A");

		StpPartnerProviderDto provider = new StpPartnerProviderDto();
		provider.setProviderTin("123456789");
		provider.setLegalEntityName("Test Entity Name");
		provider.setDoingBusinessAs("Test dba Name");
		provider.setEnrollmentList(List.of(enrollment));

		StpProviderEnrollmentRequestDto request = new StpProviderEnrollmentRequestDto();
		request.setHeader(header);
		request.setPartnerProviders(List.of(provider));
		return request;
	}

	private StpRejectReasonDto buildRejectRequest() {
		StpRejectReasonDto dto = new StpRejectReasonDto();
		dto.setNetworkPartner("STP Partner");
		dto.setOptumTransactionId("txn-123");
		dto.setRejectionReasonCode("R001");
		dto.setRejectionReason("Invalid account");
		return dto;
	}

}
