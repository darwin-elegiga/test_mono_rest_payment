package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentHeaderDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentRecordDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpPartnerProviderDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentRequestDto;
import com.uhg.optum.financials.payments.payer.model.StpEnrollmentBatch;
import com.uhg.optum.financials.payments.payer.model.StpEnrollmentRecord;
import com.uhg.optum.financials.payments.payer.repository.StpEnrollmentBatchRepository;
import com.uhg.optum.financials.payments.payer.repository.StpEnrollmentRecordRepository;
import com.uhg.optum.financials.payments.payer.service.StpEnrollmentOutboundClient;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.core.env.Environment;
import org.springframework.test.util.ReflectionTestUtils;
import org.springframework.web.client.RestTemplate;

import java.util.ArrayList;
import java.util.List;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.atLeastOnce;
import static org.mockito.Mockito.never;

@ExtendWith(MockitoExtension.class)
class StpEnrollmentServiceImplTests {

	@Mock
	private StpEnrollmentBatchRepository stpEnrollmentBatchRepository;

	@Mock
	private StpEnrollmentRecordRepository stpEnrollmentRecordRepository;

	@Mock
	private StpEnrollmentOutboundClient stpEnrollmentOutboundClient;

	@Mock
	private LogUtility logUtility;

	@Mock
	private Environment environment;

	@Mock
	private RestTemplate restTemplate;

	@InjectMocks
	private StpEnrollmentServiceImpl stpEnrollmentService;

	@Test
	void processEnrollmentRequest_validRequest_savesAndSendsToPreferencing() {
		ReflectionTestUtils.setField(this.stpEnrollmentService, "preferencingUrl", "http://preferencing");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "callbackUrl", "http://callback");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "recognizedPartners", "STP Partner");

		given(this.stpEnrollmentBatchRepository.existsByCorrelationId("287678765456")).willReturn(false);
		given(this.stpEnrollmentBatchRepository.save(any(StpEnrollmentBatch.class))).willAnswer((invocation) -> {
			StpEnrollmentBatch batch = invocation.getArgument(0);
			if (batch.getId() == null) {
				batch.setId(1L);
			}
			return batch;
		});
		given(this.stpEnrollmentRecordRepository.existsByProviderTinAndEnrollmentType("123456789", "C2B"))
			.willReturn(false);
		given(this.stpEnrollmentOutboundClient.sendToPreferencing(any(StpProviderEnrollmentRequestDto.class),
				any(String.class)))
			.willReturn(true);

		this.stpEnrollmentService.processEnrollmentRequest(buildValidRequest());

		then(this.stpEnrollmentRecordRepository).should(atLeastOnce()).saveAll(any(List.class));
		then(this.stpEnrollmentOutboundClient).should(never()).sendValidationCallback(any(), any());
		then(this.stpEnrollmentOutboundClient).should()
			.sendToPreferencing(any(StpProviderEnrollmentRequestDto.class), any(String.class));
	}

	@Test
	void processEnrollmentRequest_invalidRequest_usesProviderSpecificCallbackUrlWhenConfigured() {
		ReflectionTestUtils.setField(this.stpEnrollmentService, "callbackUrl", "http://fallback-callback");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "recognizedPartners", "STP-Partner123");

		given(this.environment.getProperty("stp.enrollment.providers.stp-partner123.callback-url"))
			.willReturn("http://static-callback/stp-partner123");
		given(this.stpEnrollmentBatchRepository.existsByCorrelationId("287678765456")).willReturn(false);
		given(this.stpEnrollmentOutboundClient.sendValidationCallback(any(), any())).willReturn(true);

		StpProviderEnrollmentRequestDto request = buildValidRequest();
		request.getHeader().setNetworkPartner("STP-Partner123");
		request.getPartnerProviders().get(0).getEnrollmentList().get(0).setAddress1(null);

		this.stpEnrollmentService.processEnrollmentRequest(request);

		then(this.stpEnrollmentOutboundClient).should()
			.sendValidationCallback(any(StpEnrollmentCallbackDto.class), eq("http://static-callback/stp-partner123"));
	}

	@Test
	void processEnrollmentRequest_invalidRequest_skipsCallbackWhenNoCallbackUrlIsConfigured() {
		ReflectionTestUtils.setField(this.stpEnrollmentService, "callbackUrl", "");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "recognizedPartners", "STP-Partner123");

		given(this.environment.getProperty("stp.enrollment.providers.stp-partner123.callback-url")).willReturn(null);
		given(this.stpEnrollmentBatchRepository.existsByCorrelationId("287678765456")).willReturn(false);

		StpProviderEnrollmentRequestDto request = buildValidRequest();
		request.getHeader().setNetworkPartner("STP-Partner123");
		request.getPartnerProviders().get(0).getEnrollmentList().get(0).setAddress1(null);

		this.stpEnrollmentService.processEnrollmentRequest(request);

		then(this.stpEnrollmentOutboundClient).should(never()).sendValidationCallback(any(), any());
	}

	@Test
	void processEnrollmentRequest_invalidRequest_sendsValidationCallback() {
		ReflectionTestUtils.setField(this.stpEnrollmentService, "preferencingUrl", "http://preferencing");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "callbackUrl", "http://callback");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "recognizedPartners", "STP Partner");

		given(this.stpEnrollmentBatchRepository.existsByCorrelationId("287678765456")).willReturn(false);
		given(this.stpEnrollmentBatchRepository.save(any(StpEnrollmentBatch.class))).willAnswer((invocation) -> {
			StpEnrollmentBatch batch = invocation.getArgument(0);
			if (batch.getId() == null) {
				batch.setId(1L);
			}
			return batch;
		});
		given(this.stpEnrollmentOutboundClient.sendValidationCallback(any(), any())).willReturn(true);

		StpProviderEnrollmentRequestDto request = buildValidRequest();
		request.getPartnerProviders().get(0).getEnrollmentList().get(0).setAddress1(null);

		this.stpEnrollmentService.processEnrollmentRequest(request);

		ArgumentCaptor<StpEnrollmentCallbackDto> callbackCaptor = ArgumentCaptor
			.forClass(StpEnrollmentCallbackDto.class);
		then(this.stpEnrollmentOutboundClient).should()
			.sendValidationCallback(callbackCaptor.capture(), any(String.class));
		then(this.stpEnrollmentRecordRepository).should(never()).saveAll(any(List.class));
		then(this.stpEnrollmentOutboundClient).should(never()).sendToPreferencing(any(), any());
		Assertions.assertThat(callbackCaptor.getValue().getHeader().getStatus()).isEqualTo("Error");
		Assertions.assertThat(callbackCaptor.getValue().getEnrollmentErrors()).hasSize(1);
		Assertions.assertThat(callbackCaptor.getValue().getEnrollmentErrors().getFirst().getErrors())
			.anyMatch((message) -> message.contains("address1"));
	}

	@Test
	void processEnrollmentRequest_invalidEnrollmentTypeAndEligibilityStatus_returnsCallbackAndSkipsPersistence() {
		ReflectionTestUtils.setField(this.stpEnrollmentService, "preferencingUrl", "http://preferencing");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "callbackUrl", "http://callback");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "recognizedPartners", "STP Partner");

		given(this.stpEnrollmentBatchRepository.existsByCorrelationId("287678765456")).willReturn(false);
		given(this.stpEnrollmentBatchRepository.save(any(StpEnrollmentBatch.class))).willAnswer((invocation) -> {
			StpEnrollmentBatch batch = invocation.getArgument(0);
			if (batch.getId() == null) {
				batch.setId(1L);
			}
			return batch;
		});
		given(this.stpEnrollmentOutboundClient.sendValidationCallback(any(), any())).willReturn(true);

		StpProviderEnrollmentRequestDto request = buildValidRequest();
		StpEnrollmentRecordDto enrollment = request.getPartnerProviders().get(0).getEnrollmentList().get(0);
		enrollment.setEnrollmentType("INVALID");
		enrollment.setEnrollmentEligibilityStatus("INVALID");

		this.stpEnrollmentService.processEnrollmentRequest(request);

		ArgumentCaptor<StpEnrollmentCallbackDto> callbackCaptor = ArgumentCaptor
			.forClass(StpEnrollmentCallbackDto.class);
		then(this.stpEnrollmentOutboundClient).should()
			.sendValidationCallback(callbackCaptor.capture(), any(String.class));
		then(this.stpEnrollmentRecordRepository).should(never()).saveAll(any(List.class));
		then(this.stpEnrollmentOutboundClient).should(never()).sendToPreferencing(any(), any());
		Assertions.assertThat(callbackCaptor.getValue().getHeader().getStatus()).isEqualTo("Error");
		Assertions.assertThat(callbackCaptor.getValue().getEnrollmentErrors()).hasSize(1);
		Assertions.assertThat(callbackCaptor.getValue().getEnrollmentErrors().getFirst().getErrors())
			.contains("enrollmentType must be C2B or B2B", "enrollmentEligibilityStatus must be Y or N");
	}

	@Test
	void processEnrollmentRequest_partialSuccess_persistsOnlyValidRecordAndReturnsValidationCallback() {
		ReflectionTestUtils.setField(this.stpEnrollmentService, "preferencingUrl", "http://preferencing");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "callbackUrl", "http://callback");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "recognizedPartners", "STP Partner");

		given(this.stpEnrollmentBatchRepository.existsByCorrelationId("287678765456")).willReturn(false);
		given(this.stpEnrollmentBatchRepository.save(any(StpEnrollmentBatch.class))).willAnswer((invocation) -> {
			StpEnrollmentBatch batch = invocation.getArgument(0);
			if (batch.getId() == null) {
				batch.setId(1L);
			}
			return batch;
		});
		given(this.stpEnrollmentRecordRepository.existsByProviderTinAndEnrollmentType("123456789", "C2B"))
			.willReturn(false);
		given(this.stpEnrollmentOutboundClient.sendValidationCallback(any(), any())).willReturn(true);
		given(this.stpEnrollmentOutboundClient.sendToPreferencing(any(StpProviderEnrollmentRequestDto.class),
				any(String.class)))
			.willReturn(true);

		StpProviderEnrollmentRequestDto request = buildValidRequest();
		request.getHeader().setCount(2);
		StpEnrollmentRecordDto invalidEnrollment = buildEnrollment("0987654321", "2234567890986757467783",
				"456 Test Lane", "", "Eden Prairie", "MN", "55435", "INVALID", "INVALID", "2023-09-25", "2023-09-25",
				"2023-10-25T23:33:00Z", "A", "Invalid enrollment");
		request.getPartnerProviders()
			.get(0)
			.setEnrollmentList(new ArrayList<>(
					List.of(request.getPartnerProviders().get(0).getEnrollmentList().get(0), invalidEnrollment)));

		this.stpEnrollmentService.processEnrollmentRequest(request);

		ArgumentCaptor<List<StpEnrollmentRecord>> savedRecordsCaptor = ArgumentCaptor.forClass(List.class);
		ArgumentCaptor<StpEnrollmentCallbackDto> callbackCaptor = ArgumentCaptor
			.forClass(StpEnrollmentCallbackDto.class);
		ArgumentCaptor<StpProviderEnrollmentRequestDto> preferencingCaptor = ArgumentCaptor
			.forClass(StpProviderEnrollmentRequestDto.class);

		then(this.stpEnrollmentRecordRepository).should(atLeastOnce()).saveAll(savedRecordsCaptor.capture());
		then(this.stpEnrollmentOutboundClient).should()
			.sendValidationCallback(callbackCaptor.capture(), any(String.class));
		then(this.stpEnrollmentOutboundClient).should()
			.sendToPreferencing(preferencingCaptor.capture(), any(String.class));

		Assertions.assertThat(savedRecordsCaptor.getAllValues().get(0)).hasSize(1);
		Assertions.assertThat(savedRecordsCaptor.getAllValues().get(0).getFirst().getEnrollmentType()).isEqualTo("C2B");
		Assertions.assertThat(callbackCaptor.getValue().getEnrollmentErrors()).hasSize(1);
		Assertions.assertThat(callbackCaptor.getValue().getEnrollmentErrors().getFirst().getErrors())
			.contains("enrollmentType must be C2B or B2B", "enrollmentEligibilityStatus must be Y or N");
		Assertions.assertThat(preferencingCaptor.getValue().getHeader().getCount()).isEqualTo(1);
		Assertions.assertThat(preferencingCaptor.getValue().getPartnerProviders()).hasSize(1);
		Assertions.assertThat(preferencingCaptor.getValue().getPartnerProviders().get(0).getEnrollmentList())
			.hasSize(1);
	}

	@Test
	void processEnrollmentRequest_missingRequiredEnrollmentFields_returnsValidationErrorsAndSkipsPersistence() {
		ReflectionTestUtils.setField(this.stpEnrollmentService, "preferencingUrl", "http://preferencing");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "callbackUrl", "http://callback");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "recognizedPartners", "STP Partner");

		given(this.stpEnrollmentBatchRepository.existsByCorrelationId("287678765456")).willReturn(false);
		given(this.stpEnrollmentBatchRepository.save(any(StpEnrollmentBatch.class))).willAnswer((invocation) -> {
			StpEnrollmentBatch batch = invocation.getArgument(0);
			if (batch.getId() == null) {
				batch.setId(1L);
			}
			return batch;
		});
		given(this.stpEnrollmentOutboundClient.sendValidationCallback(any(), any())).willReturn(true);

		StpProviderEnrollmentRequestDto request = buildValidRequest();
		StpEnrollmentRecordDto enrollment = request.getPartnerProviders().get(0).getEnrollmentList().get(0);
		enrollment.setEnrollmentType(" ");
		enrollment.setEnrollmentEligibilityStatus(" ");
		enrollment.setEnrollmentStatusCd(null);
		enrollment.setState("");
		enrollment.setZip9(null);

		this.stpEnrollmentService.processEnrollmentRequest(request);

		ArgumentCaptor<StpEnrollmentCallbackDto> callbackCaptor = ArgumentCaptor
			.forClass(StpEnrollmentCallbackDto.class);
		then(this.stpEnrollmentOutboundClient).should()
			.sendValidationCallback(callbackCaptor.capture(), any(String.class));
		then(this.stpEnrollmentRecordRepository).should(never()).saveAll(any(List.class));
		then(this.stpEnrollmentOutboundClient).should(never()).sendToPreferencing(any(), any());
		Assertions.assertThat(callbackCaptor.getValue().getEnrollmentErrors()).hasSize(1);
		Assertions.assertThat(callbackCaptor.getValue().getEnrollmentErrors().getFirst().getErrors())
			.contains("enrollmentType must be present", "enrollmentEligibilityStatus must be present",
					"enrollmentStatusCd must be present", "state must be present", "zip9 must be present");
	}

	@Test
	void processEnrollmentRequest_overlongCorrelationId_returnsCallbackAndSkipsPersistence() {
		ReflectionTestUtils.setField(this.stpEnrollmentService, "preferencingUrl", "http://preferencing");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "callbackUrl", "http://callback");
		ReflectionTestUtils.setField(this.stpEnrollmentService, "recognizedPartners", "STP Partner");

		String correlationId = "A".repeat(101);
		given(this.stpEnrollmentBatchRepository.existsByCorrelationId(correlationId)).willReturn(false);
		given(this.stpEnrollmentOutboundClient.sendValidationCallback(any(), any())).willReturn(true);

		StpProviderEnrollmentRequestDto request = buildValidRequest();
		request.getHeader().setCorrelationId(correlationId);

		this.stpEnrollmentService.processEnrollmentRequest(request);

		ArgumentCaptor<StpEnrollmentCallbackDto> callbackCaptor = ArgumentCaptor
			.forClass(StpEnrollmentCallbackDto.class);
		then(this.stpEnrollmentBatchRepository).should(never()).save(any(StpEnrollmentBatch.class));
		then(this.stpEnrollmentRecordRepository).should(never()).saveAll(any(List.class));
		then(this.stpEnrollmentOutboundClient).should(never()).sendToPreferencing(any(), any());
		then(this.stpEnrollmentOutboundClient).should()
			.sendValidationCallback(callbackCaptor.capture(), any(String.class));
		Assertions.assertThat(callbackCaptor.getValue().getHeader().getCorrelationId()).isEqualTo(correlationId);
		Assertions.assertThat(callbackCaptor.getValue().getEnrollmentErrors()).hasSize(1);
		Assertions.assertThat(callbackCaptor.getValue().getEnrollmentErrors().getFirst().getErrors())
			.contains("correlationId must not exceed 100 characters");
	}

	@Test
	void processEnrollmentRequest_duplicateCorrelation_isSkipped() {
		given(this.stpEnrollmentBatchRepository.existsByCorrelationId("287678765456")).willReturn(true);

		this.stpEnrollmentService.processEnrollmentRequest(buildValidRequest());

		then(this.stpEnrollmentBatchRepository).should(never()).save(any(StpEnrollmentBatch.class));
		then(this.stpEnrollmentRecordRepository).should(never()).saveAll(any(List.class));
		then(this.stpEnrollmentOutboundClient).should(never()).sendValidationCallback(any(), any());
		then(this.stpEnrollmentOutboundClient).should(never()).sendToPreferencing(any(), any());
	}

	private StpProviderEnrollmentRequestDto buildValidRequest() {
		StpEnrollmentHeaderDto header = new StpEnrollmentHeaderDto();
		header.setNetworkPartner("STP Partner");
		header.setCorrelationId("287678765456");
		header.setCount(1);

		StpEnrollmentRecordDto enrollment = buildEnrollment("1234567890", "1234567890986757467783", "123 Test Lane", "",
				"Eden Prairie", "MN", "55435", "C2B", "Y", "2023-09-24", "2023-09-24", "2023-10-24T23:33:00Z", "A",
				"Fraud");

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

	private StpEnrollmentRecordDto buildEnrollment(String npi, String uuid, String address1, String address2,
			String city, String state, String zip9, String enrollmentType, String enrollmentEligibilityStatus,
			String effectiveDate, String initialEnrollmentDate, String lastUpdatedUTCDate, String enrollmentStatusCd,
			String enrollmentStatusReason) {
		StpEnrollmentRecordDto enrollment = new StpEnrollmentRecordDto();
		enrollment.setNpi(npi);
		enrollment.setUuid(uuid);
		enrollment.setAddress1(address1);
		enrollment.setAddress2(address2);
		enrollment.setCity(city);
		enrollment.setState(state);
		enrollment.setZip9(zip9);
		enrollment.setEnrollmentType(enrollmentType);
		enrollment.setEnrollmentEligibilityStatus(enrollmentEligibilityStatus);
		enrollment.setEffectiveDate(effectiveDate);
		enrollment.setInitialEnrollmentDate(initialEnrollmentDate);
		enrollment.setLastUpdatedUTCDate(lastUpdatedUTCDate);
		enrollment.setEnrollmentStatusCd(enrollmentStatusCd);
		enrollment.setEnrollmentStatusReason(enrollmentStatusReason);
		return enrollment;
	}

}
