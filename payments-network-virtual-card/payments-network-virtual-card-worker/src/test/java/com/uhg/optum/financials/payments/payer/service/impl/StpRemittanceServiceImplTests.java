package com.uhg.optum.financials.payments.payer.service.impl;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.stp.StpVirtualCardRequestDto;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.PayeeDetails;
import com.uhg.optum.financials.payments.payer.model.StpEnrollmentBatch;
import com.uhg.optum.financials.payments.payer.model.StpEnrollmentRecord;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.repository.StpEnrollmentBatchRepository;
import com.uhg.optum.financials.payments.payer.repository.StpEnrollmentRecordRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.StpProviderClient;
import com.uhg.optum.financials.shared.client.constant.PaymentType;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.mock.env.MockEnvironment;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.Collections;
import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.never;

@ExtendWith(MockitoExtension.class)
class StpRemittanceServiceImplTests {

	@Mock
	private StpEnrollmentRecordRepository stpEnrollmentRecordRepository;

	@Mock
	private StpEnrollmentBatchRepository stpEnrollmentBatchRepository;

	@Mock
	private SubAccountRepository subAccountRepository;

	@Mock
	private StpProviderClient stpProviderClient;

	@Mock
	private ResilientRestClientFactory resilientRestClientFactory;

	@Mock
	private ResilientRestClient claimClient;

	private StpRemittanceServiceImpl service;

	private ObjectMapper objectMapper;

	private static final String CLAIM_JSON = """
			{
			  "claimLevelInformation": [
			    {
			      "claimLoop": [
			        {
			          "claimPaymentInformation": {
			            "patientControlNumber": "PCN12345",
			            "paymentAmount": 200.0
			          },
			          "involvedEntitiesInfo": [
			            {
			              "entityIdentifierCode": "QC",
			              "entityTypeQualifier": "1",
			              "firstName": "John",
			              "lastName": "Doe",
			              "dob": "19800101",
			              "patientAccountNumber": "PA123"
			            },
			            {
			              "entityIdentifierCode": "82",
			              "entityTypeQualifier": "1",
			              "firstName": "Provider",
			              "lastName": "One"
			            }
			          ],
			          "relevantDates": [
			            {
			              "qualifier": "050",
			              "date": "2026-06-20"
			            },
			            {
			              "qualifier": "573",
			              "date": "2026-06-22"
			            }
			          ],
			          "loop2110": [
			            {
			              "serviceDate": [
			                {
			                  "dateTimeQualifier": "472",
			                  "date": "2026-05-15"
			                }
			              ]
			            }
			          ]
			        }
			      ]
			    }
			  ]
			}
			""";

	@BeforeEach
	void setUp() {
		MockEnvironment environment = new MockEnvironment()
			.withProperty("stp.remittance.providers.stp-partner.endpoint", "https://stp-partner/remittance")
			.withProperty("stp.remittance.providers.stp-partner.auth-type", "oauth");

		this.objectMapper = new ObjectMapper();
		given(this.resilientRestClientFactory.getClient("stp-claim")).willReturn(this.claimClient);
		this.service = new StpRemittanceServiceImpl(this.stpEnrollmentRecordRepository,
				this.stpEnrollmentBatchRepository, environment, this.objectMapper, this.subAccountRepository,
				this.stpProviderClient, this.resilientRestClientFactory);
	}

	// HAPPY PATH TESTS
	@Test
	void sendRemittance_b2b_withDirectJsonInClmUrl() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);

		setupEnrollmentAndBatch();
		this.service.sendRemittance(paymentRequest);

		verifyRemittancePosted();
		verifyConsumerNotPresent();
	}

	@Test
	void sendRemittance_b2b_withPayerName() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);

		SubAccount subAccount = new SubAccount();
		subAccount.setName("123 Business St");

		given(this.subAccountRepository.findByOrgShortCodeAndSubAccountShortCodeAndAccountShortCode("ORG1", null, null))
			.willReturn(subAccount);
		setupEnrollmentAndBatch();

		this.service.sendRemittance(paymentRequest);

		ArgumentCaptor<StpVirtualCardRequestDto> captor = ArgumentCaptor.forClass(StpVirtualCardRequestDto.class);
		then(this.stpProviderClient).should().postRemittance(any(), any(), captor.capture(), any(), any());

		assertThat(captor.getValue().getPayer().getName()).isEqualTo("123 Business St");
	}

	@Test
	void sendRemittance_b2b_excludesConsumer() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);
		paymentRequest.getPaymentDetails().setType(PaymentType.NOTIFICATION);

		setupEnrollmentAndBatch();
		this.service.sendRemittance(paymentRequest);

		ArgumentCaptor<StpVirtualCardRequestDto> captor = ArgumentCaptor.forClass(StpVirtualCardRequestDto.class);
		then(this.stpProviderClient).should().postRemittance(any(), any(), captor.capture(), any(), any());

		assertThat(captor.getValue().getConsumer()).isNull();
		assertThat(captor.getValue().getPayee()).isNotNull();
	}

	@Test
	void sendRemittance_multipleEnrollmentRecords_usesLatest() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);

		StpEnrollmentRecord record1 = createEnrollmentRecord();
		record1.setUpdatedAt(LocalDateTime.of(2026, 6, 1, 10, 0));
		StpEnrollmentRecord record2 = createEnrollmentRecord();
		record2.setUpdatedAt(LocalDateTime.of(2026, 6, 22, 15, 30));

		given(this.stpEnrollmentRecordRepository.findAll()).willReturn(List.of(record1, record2));
		given(this.stpEnrollmentBatchRepository.findById(1L)).willReturn(Optional.of(createBatch()));
		given(this.subAccountRepository.findByOrgShortCodeAndSubAccountShortCodeAndAccountShortCode(any(), any(),
				any()))
			.willReturn(null);

		this.service.sendRemittance(paymentRequest);

		verifyRemittancePosted();
	}

	// VALIDATION & SKIP TESTS
	@Test
	void sendRemittance_skips_whenClmUrlIsEmpty() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(null);

		this.service.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	@Test
	void sendRemittance_skips_whenClmUrlIsBlank() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl("   ");

		this.service.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	@Test
	void sendRemittance_skips_whenNetworkPaymentIdMissing() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);
		paymentRequest.getPaymentDetails().setNetworkPaymentId(null);

		this.service.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	@Test
	void sendRemittance_skips_whenPaymentDetailsNull() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);
		paymentRequest.setPaymentDetails(null);

		this.service.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	@Test
	void sendRemittance_defaultsToB2b_whenPaymentTypeIsNull() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);
		paymentRequest.getPaymentDetails().setType(null);

		setupEnrollmentAndBatch();
		this.service.sendRemittance(paymentRequest);

		verifyRemittancePosted();
		verifyConsumerNotPresent();
	}

	@Test
	void sendRemittance_skips_whenPayeeDetailsNull() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);
		paymentRequest.setPayeeDetails(null);

		this.service.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	@Test
	void sendRemittance_skips_whenEnrollmentNotFound() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);

		given(this.stpEnrollmentRecordRepository.findAll()).willReturn(Collections.emptyList());

		this.service.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	@Test
	void sendRemittance_skips_whenEnrollmentNotEligible() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);

		StpEnrollmentRecord record = createEnrollmentRecord();
		record.setEnrollmentEligibilityStatus("N");

		given(this.stpEnrollmentRecordRepository.findAll()).willReturn(List.of(record));

		this.service.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	@Test
	void sendRemittance_skips_whenEnrollmentStatusNotActive() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);

		StpEnrollmentRecord record = createEnrollmentRecord();
		record.setEnrollmentStatusCd("I");

		given(this.stpEnrollmentRecordRepository.findAll()).willReturn(List.of(record));

		this.service.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	@Test
	void sendRemittance_skips_whenProviderEndpointNotConfigured() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);

		StpEnrollmentRecord record = createEnrollmentRecord();
		given(this.stpEnrollmentRecordRepository.findAll()).willReturn(List.of(record));
		given(this.stpEnrollmentBatchRepository.findById(1L)).willReturn(Optional.of(createBatch()));

		MockEnvironment emptyEnv = new MockEnvironment();
		StpRemittanceServiceImpl serviceWithMissingConfig = new StpRemittanceServiceImpl(
				this.stpEnrollmentRecordRepository, this.stpEnrollmentBatchRepository, emptyEnv, this.objectMapper,
				this.subAccountRepository, this.stpProviderClient, this.resilientRestClientFactory);

		serviceWithMissingConfig.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	@Test
	void sendRemittance_skips_whenClaimJsonIsInvalid() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl("{ invalid json }");

		this.service.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	@Test
	void sendRemittance_passesEndpointAndAuthTypeToProviderClient() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);

		setupEnrollmentAndBatch();

		this.service.sendRemittance(paymentRequest);

		ArgumentCaptor<StpProviderClient.ProviderConfig> configCaptor = ArgumentCaptor
			.forClass(StpProviderClient.ProviderConfig.class);
		then(this.stpProviderClient).should().postRemittance(any(), configCaptor.capture(), any(), any(), any());
		assertThat(configCaptor.getValue().endpoint()).isEqualTo("https://stp-partner/remittance");
		assertThat(configCaptor.getValue().authType()).isEqualTo("oauth");
	}

	@Test
	void sendRemittance_skips_whenTaxIdDoesNotMatch() {
		PaymentRequest paymentRequest = createPaymentRequest();
		paymentRequest.setClmUrl(CLAIM_JSON);
		paymentRequest.getPayeeDetails().setTaxId("999999999");

		given(this.stpEnrollmentRecordRepository.findAll()).willReturn(Collections.emptyList());

		this.service.sendRemittance(paymentRequest);

		then(this.stpProviderClient).should(never()).postRemittance(any(), any(), any(), any(), any());
	}

	// HELPER METHODS
	private PaymentRequest createPaymentRequest() {
		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setOrgShortCode("ORG1");
		paymentRequest.setSubAccountShortCode(null);
		paymentRequest.setAccountShortCode(null);
		paymentRequest.setPayr835Id("TPA1");

		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setNetworkPaymentId("NPID-1001");
		paymentDetails.setIssuerPaymentId("ISSUER-1001");
		paymentDetails.setReferenceId("REF-1001");
		paymentDetails.setAmount(new BigDecimal("2479.81"));
		paymentDetails.setType(PaymentType.PAYMENT);
		paymentRequest.setPaymentDetails(paymentDetails);

		PayeeDetails payeeDetails = new PayeeDetails();
		payeeDetails.setType("B2B");
		payeeDetails.setTaxId("123456789");
		payeeDetails.setName("ABC Medical Group");
		payeeDetails.setReceiverId("RCV123");
		paymentRequest.setPayeeDetails(payeeDetails);

		return paymentRequest;
	}

	private StpEnrollmentRecord createEnrollmentRecord() {
		StpEnrollmentRecord record = new StpEnrollmentRecord();
		record.setProviderTin("123456789");
		record.setEnrollmentType("B2B");
		record.setEnrollmentEligibilityStatus("Y");
		record.setEnrollmentStatusCd("A");
		record.setUpdatedAt(LocalDateTime.now());
		record.setStpEnrollmentBatchId(1L);
		return record;
	}

	private StpEnrollmentBatch createBatch() {
		StpEnrollmentBatch batch = new StpEnrollmentBatch();
		batch.setNetworkPartner("STP Partner");
		return batch;
	}

	private void setupEnrollmentAndBatch() {
		StpEnrollmentRecord record = createEnrollmentRecord();
		record.setEnrollmentType("B2B");

		given(this.stpEnrollmentRecordRepository.findAll()).willReturn(List.of(record));
		given(this.stpEnrollmentBatchRepository.findById(1L)).willReturn(Optional.of(createBatch()));
	}

	private void verifyRemittancePosted() {
		then(this.stpProviderClient).should()
			.postRemittance(any(), any(StpProviderClient.ProviderConfig.class), any(), any(), any());
	}

	private void verifyConsumerNotPresent() {
		ArgumentCaptor<StpVirtualCardRequestDto> captor = ArgumentCaptor.forClass(StpVirtualCardRequestDto.class);
		then(this.stpProviderClient).should()
			.postRemittance(any(), any(StpProviderClient.ProviderConfig.class), captor.capture(), any(), any());

		StpVirtualCardRequestDto payload = captor.getValue();
		assertThat(payload.getConsumer()).isNull();
	}

}
