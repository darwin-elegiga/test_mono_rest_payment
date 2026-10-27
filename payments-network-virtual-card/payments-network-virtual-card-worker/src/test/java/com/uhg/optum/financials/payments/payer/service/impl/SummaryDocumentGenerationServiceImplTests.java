package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.BlobServiceClient;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.BinRange;
import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.document.DocumentRequest;
import com.uhg.optum.financials.payments.payer.dto.document.DocumentRequestCDE;
import com.uhg.optum.financials.payments.payer.dto.document.DocumentResponse;
import com.uhg.optum.financials.payments.payer.dto.document.SummaryDocumentDto;
import com.uhg.optum.financials.payments.payer.dto.document.SummaryDocumentInputDto;
import com.uhg.optum.financials.payments.payer.dto.document.TemplateMappedValues;
import com.uhg.optum.financials.payments.payer.enums.LogoType;
import com.uhg.optum.financials.payments.payer.model.Address;
import com.uhg.optum.financials.payments.payer.model.DocumentCustomRemarks;
import com.uhg.optum.financials.payments.payer.model.LogoDetails;
import com.uhg.optum.financials.payments.payer.model.PayeeDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.ShippingDetails;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.repository.CompanyLogoDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.DocumentCustomRemarksRepository;
import com.uhg.optum.financials.payments.payer.repository.DocumentResponseDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.LogoDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.BarCodeService;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import net.minidev.json.JSONObject;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockedStatic;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.test.util.ReflectionTestUtils;
import org.springframework.util.MultiValueMap;
import org.springframework.util.ResourceUtils;
import org.springframework.web.client.RestClient;

import java.io.File;
import java.io.FileNotFoundException;
import java.math.BigDecimal;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicReference;
import java.util.function.Function;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;

@ExtendWith(MockitoExtension.class)

public class SummaryDocumentGenerationServiceImplTests {

	@Mock
	private ResilientRestClientFactory resilientRestClientFactory;

	@Mock
	private ResilientRestClient documentGenerationClient;

	@Mock
	private RestClient restClient;

	@Mock
	private RestClient.RequestBodyUriSpec requestBodyUriSpec;

	@Mock
	private RestClient.ResponseSpec responseSpec;

	@InjectMocks
	private SummaryPageServiceImpl documentGenerationService;

	@Mock
	DocumentResponseDetailsRepository documentResponseDetailsRepository;

	@Mock
	DocumentCustomRemarksRepository documentCustomRemarksRepository;

	@Mock
	SubAccountRepository subAccountRepository;

	@Mock
	LogUtility logUtility;

	@Mock
	PaymentRequestRepository paymentRequestRepository;

	@Mock
	LogoDetailsRepository logoDetailsRepository;

	@Mock
	CompanyLogoDetailsRepository companyLogoDetailsRepository;

	@Mock
	BarCodeService barCodeService;

	@Mock
	EventBusProducer eventBusProducer;

	@Mock
	VirtualCardUtil virtualCardUtil;

	DocumentCustomRemarks documentCustomRemarks;

	@Value("${template.file.name}")
	String templateFileName;

	@Mock
	PaymentRequest paymentRequestClass;

	@Mock
	SubAccount subAccount;

	@Mock
	ZetaApiService zetaApiService;

	@Mock
	BlobServiceClient blobServiceClient;

	@Mock
	BlobContainerClient blobContainerClient;

	@Mock
	BlobClient blobClient;

	@BeforeEach
	void setUp() {
		Mockito.lenient()
			.when(this.resilientRestClientFactory.getClient("documentGeneration"))
			.thenReturn(this.documentGenerationClient);
		ReflectionTestUtils.setField(this.documentGenerationService, "documentGenerationClient",
				this.documentGenerationClient);
	}

	private PaymentRequest getMockPaymentRequest() {

		PaymentRequest loadPaymentRequest = new PaymentRequest();
		loadPaymentRequest.setAccountShortCode("test");
		loadPaymentRequest.setSubAccountShortCode("Test");
		loadPaymentRequest.setOrgShortCode("123");
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setAmount(new BigDecimal(100));
		paymentDetails.setPaymentDate(LocalDate.now());
		loadPaymentRequest.setPaymentDetails(paymentDetails);
		PayeeDetails payeeDetails = new PayeeDetails();
		payeeDetails.setTaxId("123456789");

		Address addressDetails = setAddress();
		payeeDetails.setAddress(addressDetails);
		loadPaymentRequest.setPayeeDetails(payeeDetails);

		SubAccount subAccount1 = new SubAccount();
		subAccount1.setName("subaccount");
		subAccount1.setAddress(addressDetails);
		ShippingDetails shippingDetails = new ShippingDetails();
		shippingDetails.setAddress(addressDetails);
		loadPaymentRequest.setShippingDetails(shippingDetails);
		loadPaymentRequest.setDocuments(new ArrayList<>());

		return loadPaymentRequest;
	}

	private SummaryDocumentInputDto getMockSummaryDoc() {
		return new SummaryDocumentInputDto("org", "acc", "subacc", "payer", "123456789", "372983721932", "3244", "ER",
				"adr1", "adr2", "adr3", "adr4", "adr5", "adr6", LocalDate.now(), "6161646");
	}

	private Address setAddress() {
		Address addressDetails = new Address();
		addressDetails.setLine1("test");
		addressDetails.setCity("US");
		addressDetails.setState("MS");
		addressDetails.setPostalCode("123");
		addressDetails.setLine2("test");
		addressDetails.setCountry("US");
		return addressDetails;
	}

	private SubAccount getSubAccount() {
		SubAccount subAccount = new SubAccount();
		subAccount.setAccountShortCode("test");
		subAccount.setSubAccountShortCode("Test");
		subAccount.setOrgShortCode("123");
		Address addressDetails = setAddress();
		subAccount.setAddress(addressDetails);
		return subAccount;
	}

	// @Test
	public void testGenerateUsingMultiPartRequest_successfulDocumentGeneration() throws Exception {
		try (MockedStatic<ResourceUtils> mockedStatic = Mockito.mockStatic(ResourceUtils.class)) {
			SummaryDocumentDto summaryDocumentDto = new SummaryDocumentDto();
			List<String> issuerPaymentIds = new ArrayList<>();
			issuerPaymentIds.add("IPId");
			summaryDocumentDto.setIssuerPaymentIds(issuerPaymentIds);
			summaryDocumentDto.setCorrelationId("123");
			summaryDocumentDto.setNetworkPaymentId("NpiId");
			summaryDocumentDto.setConsolidatedPaymentAmount(new BigDecimal(100));
			// Prepare test data
			ReflectionTestUtils.setField(this.documentGenerationService, "documentGenerationServiceUrl", "/test");
			ReflectionTestUtils.setField(this.documentGenerationService, "templateFileName", "/test");

			this.documentCustomRemarks = new DocumentCustomRemarks();
			this.documentCustomRemarks.setCustomRemarks1("test");
			this.documentCustomRemarks.setCustomRemarks2("test");

			this.documentCustomRemarks.setOrgShortCode("123");
			this.documentCustomRemarks.setAccountShortCode("123");
			this.documentCustomRemarks.setSubAccountShortCode("123");

			LogoDetails logoDetails = new LogoDetails();
			logoDetails.setLogoUrl("test");
			logoDetails.setAccountShortCode("acc");
			logoDetails.setOrgShortCode("org");
			logoDetails.setSubAccountShortCode("subacc");
			logoDetails.setType("test");
			logoDetails.setLogoId(UUID.randomUUID());

			List<DocumentCustomRemarks> documentCustomRemarksList = new ArrayList<>();
			documentCustomRemarksList.add(this.documentCustomRemarks);
			SummaryDocumentInputDto summaryDocumentInputDto = getMockSummaryDoc();
			Mockito.when(this.paymentRequestRepository.findDocDetailsByIssuerPaymentIdAndCorrelationId(any(), any()))
				.thenReturn(summaryDocumentInputDto);
			Mockito.when(this.documentCustomRemarksRepository.findByCustomRemarks1NotNull())
				.thenReturn(documentCustomRemarksList);
			Mockito
				.when(this.logoDetailsRepository.findLogoUrlByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(
						Mockito.anyString(), Mockito.anyString(), Mockito.anyString()))
				.thenReturn(logoDetails.getLogoUrl());
			Mockito.when(this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.MASTERCARD.name()))
				.thenReturn(logoDetails.getLogoUrl());
			Mockito.when(this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.OPTUM.name()))
				.thenReturn(logoDetails.getLogoUrl());
			File mockFile = new File("valid/path/to/resource");
			try {
				Mockito.when(ResourceUtils.getFile(this.templateFileName)).thenReturn(mockFile);
			}
			catch (FileNotFoundException ex) {
				throw new RuntimeException(ex);
			}

			DocumentResponse documentResponse = new DocumentResponse();

			JSONObject metaData = new JSONObject();
			documentResponse.setMetadata(metaData);
			documentResponse.setDocumentUrl("doc/url");
			ObjectMapper objectMapper = new ObjectMapper();
			String documentResponseAsString = null;
			try {
				documentResponseAsString = objectMapper.writeValueAsString(documentResponse);
			}
			catch (JsonProcessingException ex) {
				throw new RuntimeException(ex);
			}
			List<DocumentCustomRemarks> customRemarksList = new ArrayList<>();
			customRemarksList.add(new DocumentCustomRemarks());
			Mockito.when(this.documentCustomRemarksRepository.findByCustomRemarks1NotNull())
				.thenReturn(customRemarksList);
			Mockito
				.when(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(anyString(),
						anyString(), anyString()))
				.thenReturn(Optional.of(getSubAccount()));
			ResponseEntity<String> response = new ResponseEntity<>("{\n"
					+ "  \"cardID\": \"resource://6032f882-a1b9-4110-be97-aed8eb1efcc1_7038392d-5fc8-44ea-bc1a-ad2c71cdb98c\",\n"
					+ "  \"cardHolderID\": null,\n" + "  \"crn\": \"180034306805\",\n"
					+ "  \"cardType\": \"PHYSICAL\",\n" + "  \"maskedPan\": \"532426-xxxxxx-8701\",\n"
					+ "  \"cardStatus\": \"ENABLED\",\n" + "  \"statusReason\": {\n" + "    \"code\": \"OTHER\",\n"
					+ "    \"description\": \"Artefact Activated\"\n" + "  },\n" + "  \"orderDetails\": {\n"
					+ "    \"orderID\": \"98ebe905-5c90-49d9-ab0c-b6e64a9ef95dFPO\",\n"
					+ "    \"cardSkuID\": \"SKUUSMCOPTUM001\",\n" + "    \"cardSku\": {\n"
					+ "      \"cardSkuId\": \"SKUUSMCOPTUM001\"\n" + "    },\n" + "    \"plasticCode\": \"OPTUMMC\",\n"
					+ "    \"thirdLineEmbossing\": \"TEST CARD 61568\",\n"
					+ "    \"fourthLineEmbossing\": \"Joseph\",\n" + "    \"expiry\": {\n" + "      \"month\": \"5\",\n"
					+ "      \"year\": \"30\"\n" + "    },\n" + "    \"deliveryAddress\": {\n"
					+ "      \"name\": \"Farzan Shaikh\",\n" + "      \"addressLine1\": \"Directiplex\",\n"
					+ "      \"addressLine2\": \"Near Andheri Subway\",\n"
					+ "      \"addressLine3\": \"Old Nagardas Road\",\n" + "      \"addressLine4\": \"Andheri East\",\n"
					+ "      \"city\": \"Mumbai\",\n" + "      \"state\": \"Maharashtra\",\n"
					+ "      \"country\": \"India\",\n" + "      \"postalCode\": \"755023\",\n"
					+ "      \"contactNumber\": \"+919090909090\"\n" + "    },\n" + "    \"tenantAttributes\": {},\n"
					+ "    \"vendorAttributes\": {},\n" + "    \"additionalAttributes\": {\n"
					+ "      \"cashlimit\": \"30001\",\n" + "      \"homephone\": \"9930158052\",\n"
					+ "      \"landmark1\": \"mazgaon\",\n" + "      \"landmark2\": \"anjeerwadi\",\n"
					+ "      \"resmailid\": \"idristinwala53@gmail.com\",\n"
					+ "      \"trackingId\": \"00000000000\",\n" + "      \"courierName\": \"IP\",\n"
					+ "      \"creditlimit\": \"1000001\",\n" + "      \"officephone\": \"2307899\",\n"
					+ "      \"productName\": \"QACARD\",\n" + "      \"accountnumber\": \"30315183\",\n"
					+ "      \"promodiscount\": \"999\",\n" + "      \"officialmailid\": \"idristi@zeta.tech\"\n"
					+ "    },\n" + "    \"orderStatus\": \"EMBOSSING_FILE_GENERATED\",\n" + "    \"files\": {\n"
					+ "      \"EMBOSSING\": {\n" + "        \"fileId\": \"760cf590-eb64-452f-bbe0-834959860d1e\",\n"
					+ "        \"fileName\": \"OPTUM_FINANCIAL.20250530012150.zip\",\n"
					+ "        \"backupFileName\": \"used/OPTUM_FINANCIAL.EMB.20250530012150.pgp\",\n"
					+ "        \"fileGenerationTimeStamp\": \"2025-05-29T19:51:51.442796496Z[UTC]\"\n" + "      }\n"
					+ "    },\n" + "    \"issuanceType\": \"NEW_ISSUANCE\"\n" + "  },\n" + "  \"vectors\": [\n"
					+ "    \"vector://BUNDLE/0e6174cb-2b98-4691-9618-be89ca6f7009\",\n"
					+ "    \"vector://ACCOUNTHOLDER/dd72cc75-8102-4f23-b905-3d8d96ee06ac\",\n"
					+ "    \"vector://RESOURCE/6032f882-a1b9-4110-be97-aed8eb1efcc1\"\n" + "  ],\n"
					+ "  \"tenantAttributes\": {},\n" + "  \"binRange\": {\n" + "    \"bin\": \"532426\",\n"
					+ "    \"network\": \"MASTERCARD\",\n"
					+ "    \"rangeID\": \"90efb901-365d-429b-a399-dc3f09f9d7eb\",\n" + "    \"binLength\": 6,\n"
					+ "    \"binType\": \"PREPAID\"\n" + "  },\n" + "  \"tenantID\": 600710,\n"
					+ "  \"sensitiveView\": {\n" + "    \"algo\": \"ECDH\",\n" + "    \"value\": {\n"
					+ "      \"serverPublicKey\": \"MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEGWr/3iFDk08O/ugs4opkmY9GX9M29XcuFIUeiGhLxKBv+/1EDt+yVCmQIG+pumOV9fABv8/en3OILZl+KeMxKg==\",\n"
					+ "      \"encryptedData\": \"DkOmqamlgVoM8MssdwGZLgCilvvIkj5/aILPQdiW0b7gQ95xXlRZbkeoHTaD99On9ANdxvQaWQizZHmi/2Kp7RN3nVN6t4rJ8k2eXZ+3h6kKjkcsZxucUYOXNATf2tJb70WjwA58PZatq/I2HJwgZMfVsdsJkVDB/p2znMako0s8k8IvSpyV7P/Y/T9iwLs9CIjivBma1YDAYj8wULyL+bPOHmXnCaSDkc3LUdEQS3y9Lh0a6XVv2gLPfDMUOjM=\",\n"
					+ "      \"iv\": \"j+1HTLhqwRH2u5+kBTZxwg==\"\n" + "    }\n" + "  },\n"
					+ "  \"cguid\": \"7038392d-5fc8-44ea-bc1a-ad2c71cdb98c\",\n" + "  \"tags\": [\n"
					+ "    \"tag://ordermechanism/reserve\",\n"
					+ "    \"tag://APPLICATION/3bb6ffa7-68a3-40f5-aa59-441bdd21496d\",\n"
					+ "    \"tag://beneficiaryCardID/53b11842-421c-4adc-9dfb-3dfb62f9f7ef\",\n"
					+ "    \"tag://PRODUCTBUNDLE/BUNUSDD002?bluePrintCode=PDDAUS001002&bundleProviderCode=BPRUS001001\"\n"
					+ "  ],\n" + "  \"validFromMMYY\": null,\n" + "  \"expiryMMYY\": \"0530\",\n"
					+ "  \"systemAttributes\": {\n" + "    \"associatedAt\": \"2025-05-29T19:22:35.334644052Z[UTC]\",\n"
					+ "    \"cardStatusModifiedAt\": \"2025-05-29T19:30:00.543823775Z[UTC]\",\n"
					+ "    \"incorrectPinCount\": \"0\",\n" + "    \"incorrectPinDailyCount\": \"0\"\n" + "  },\n"
					+ "  \"cardLifecycleID\": \"7038392d-5fc8-44ea-bc1a-ad2c71cdb98c\",\n"
					+ "  \"issuanceType\": \"NEW_ISSUANCE\",\n" + "  \"additionalAttributes\": {\n"
					+ "    \"identities\": {}\n" + "  },\n" + "  \"baseCardInfo\": null\n" + "}", HttpStatus.OK);
			Mockito.when(this.zetaApiService.getVirtualCard(any())).thenReturn(response);
			PaymentRequest paymentRequest = new PaymentRequest();
			ShippingDetails shippingDetails = new ShippingDetails();
			Address address = new Address();
			address.setLine1("test");
			address.setCountry("US");
			address.setState("Newyork");
			address.setCity("NY");
			shippingDetails.setAddress(address);
			paymentRequest.setShippingDetails(shippingDetails);
			PaymentDetails paymentDetailsDto = new PaymentDetails();
			paymentDetailsDto.setAmount(new BigDecimal(100));
			paymentRequest.setPaymentDetails(paymentDetailsDto);
			Mockito.when(this.blobServiceClient.getBlobContainerClient(any())).thenReturn(this.blobContainerClient);
			Mockito.when(this.blobContainerClient.getBlobClient(any())).thenReturn(this.blobClient);
			ReflectionTestUtils.setField(this.documentGenerationService, "imagePath", "images");
			this.documentGenerationService.processSummaryDocumentMessage(paymentRequest, LocalDate.now(),
					UUID.randomUUID());
			// Mockito.verify(this.documentResponseDetailsRepository).save(Mockito.any(DocumentResponseDetails.class));
			// mockedStatic.clearInvocations();
			Assertions.assertThat(this.documentGenerationService).isNotNull();
		}

	}

	@Test
	void processSummaryDocumentMessage_success() throws Exception {
		Path tempTemplate = Files.createTempFile("vc-template-success", ".html");
		PaymentRequest paymentRequest = BDDMockito.mock(PaymentRequest.class);
		PaymentDetails paymentDetails = BDDMockito.mock(PaymentDetails.class);
		SummaryDocumentInputDto summaryDocumentInputDto = getMockSummaryDoc();
		DocumentCustomRemarks remarks = new DocumentCustomRemarks();
		remarks.setCustomRemarks1("Payment for [payer name]");
		remarks.setCustomRemarks2("1. Call [payer phone] for support.");
		SubAccount subAccount = getSubAccount();
		subAccount.setName("subaccount");
		subAccount.setReturnMailingAddress(setAddress());
		subAccount.setClaimOfficePhoneNumber("1234567890");
		subAccount.setPhoneNumber("1234567890");

		CardDetailsDTO cardDetailsDTO = new CardDetailsDTO();
		cardDetailsDTO.setMaskedPan("532426-xxxxxx-8701");
		cardDetailsDTO.setExpiryMMYY("1130");
		BinRange binRange = new BinRange();
		binRange.setBin("532426");
		cardDetailsDTO.setBinRange(binRange);

		BDDMockito.when(paymentRequest.getPaymentDetails()).thenReturn(paymentDetails);
		BDDMockito.when(paymentRequest.getShippingDetails())
			.thenReturn(BDDMockito.mock(com.uhg.optum.financials.payments.payer.model.ShippingDetails.class));
		BDDMockito.when(paymentRequest.getShippingDetails().getAddress())
			.thenReturn(BDDMockito.mock(com.uhg.optum.financials.payments.payer.model.Address.class));
		BDDMockito.when(paymentDetails.getIssuerPaymentId()).thenReturn("issuerId");
		BDDMockito.when(paymentRequest.getCorrelationId()).thenReturn("corrId");
		BDDMockito.when(paymentDetails.getAmount()).thenReturn(BigDecimal.TEN);
		BDDMockito.when(paymentDetails.getPin()).thenReturn("1234");
		BDDMockito.when(paymentDetails.getId()).thenReturn("payId");
		BDDMockito
			.when(this.paymentRequestRepository.findDocDetailsByIssuerPaymentIdAndCorrelationId(anyString(),
					anyString()))
			.thenReturn(summaryDocumentInputDto);
		BDDMockito
			.when(this.documentCustomRemarksRepository
				.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode(anyString(), anyString(), anyString()))
			.thenReturn(remarks);
		BDDMockito
			.when(this.logoDetailsRepository.findLogoUrlByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(
					anyString(), anyString(), anyString()))
			.thenReturn("https://logo");
		BDDMockito.when(this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.MASTERCARD.name()))
			.thenReturn("https://mc-logo");
		BDDMockito.when(this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.OPTUM.name()))
			.thenReturn("https://optum-logo");
		BDDMockito.when(this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.INSTRUCTIONS.name()))
			.thenReturn("https://instructions-logo");
		BDDMockito
			.when(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(anyString(),
					anyString(), anyString()))
			.thenReturn(Optional.of(subAccount));
		BDDMockito.when(this.barCodeService.generateBarCode(any())).thenReturn("barcode-url");
		BDDMockito.when(this.zetaApiService.getCardDetails(any())).thenReturn(cardDetailsDTO);
		BDDMockito.when(this.documentGenerationClient.execute(any())).thenAnswer((invocation) -> {
			@SuppressWarnings("unchecked")
			Function<RestClient, ResponseEntity<String>> callback = invocation.getArgument(0);
			return callback.apply(this.restClient);
		});
		BDDMockito.when(this.restClient.post()).thenReturn(this.requestBodyUriSpec);
		BDDMockito.when(this.requestBodyUriSpec.uri(anyString())).thenReturn(this.requestBodyUriSpec);
		BDDMockito.when(this.requestBodyUriSpec.headers(any())).thenReturn(this.requestBodyUriSpec);
		BDDMockito.doReturn(this.requestBodyUriSpec)
			.when(this.requestBodyUriSpec)
			.body(Mockito.any(MultiValueMap.class));
		BDDMockito.when(this.requestBodyUriSpec.retrieve()).thenReturn(this.responseSpec);
		BDDMockito.when(this.responseSpec.toEntity(String.class))
			.thenReturn(new ResponseEntity<>(
					"{\"documentId\":\"doc-1\",\"documentUrl\":\"https://doc\",\"metadata\":{},\"documentType\":\"pdf\"}",
					HttpStatus.OK));
		ReflectionTestUtils.setField(this.documentGenerationService, "templateFileName", tempTemplate.toString());
		ReflectionTestUtils.setField(this.documentGenerationService, "documentGenerationServiceUrl",
				"http://localhost/mock-doc");
		ReflectionTestUtils.setField(this.documentGenerationService, "imagePath", "missing-images");

		this.documentGenerationService.processSummaryDocumentMessage(paymentRequest, LocalDate.now(),
				UUID.randomUUID());

		BDDMockito.verify(this.paymentRequestRepository)
			.findDocDetailsByIssuerPaymentIdAndCorrelationId(anyString(), anyString());
	}

	@Test
	void formatCustomRemarksForHtml_shouldConvertActualAndEscapedNewlinesToBr() {
		String input = "Line1\\nLine2\nLine3\r\nLine4\rLine5";

		String result = ReflectionTestUtils.invokeMethod(this.documentGenerationService, "formatCustomRemarksForHtml",
				input);

		Assertions.assertThat(result).isEqualTo("Line1<br/>Line2<br/>Line3<br/>Line4<br/>Line5");
	}

	@Test
	void formatCustomRemarksForHtml_shouldConvertUrlsToHyperlinks() {
		String input = "Please visit: https://payments.optum.com\nFor more info: http://example.com/path?q=1\nAlso see: www.optum.com";

		String result = ReflectionTestUtils.invokeMethod(this.documentGenerationService, "formatCustomRemarksForHtml",
				input);

		Assertions.assertThat(result)
			.contains("<a href=\"https://payments.optum.com\" target=\"_blank\">https://payments.optum.com</a>")
			.contains("<a href=\"http://example.com/path?q=1\" target=\"_blank\">http://example.com/path?q=1</a>")
			.contains("<a href=\"www.optum.com\" target=\"_blank\">www.optum.com</a>")
			.contains("<br/>");
	}

	@Test
	void formatCustomRemarksForHtml_shouldReturnNullForNullInput() {
		String result = ReflectionTestUtils.invokeMethod(this.documentGenerationService, "formatCustomRemarksForHtml",
				(Object) null);
		Assertions.assertThat(result).isNull();
	}

	@Test
	void processSummaryDocumentMessage_shouldSendFormattedCustomRemarks1InPayload() throws Exception {
		Path tempTemplate = Files.createTempFile("vc-template", ".html");
		LocalDate issueDate = LocalDate.of(2026, 2, 3);
		LocalDate paymentExpiryDate = LocalDate.of(2026, 11, 9);
		SummaryDocumentInputDto summaryDocumentInputDto = new SummaryDocumentInputDto("org", "acc", "subacc", "payer",
				"123456789", "372983721932", "3244", "ER", "adr1", "adr2", "adr3", "adr4", "adr5", "adr6", issueDate,
				"6161646");

		DocumentCustomRemarks remarks = new DocumentCustomRemarks();
		remarks.setCustomRemarks1(
				"The document you are holding is a payment for services provided. This document is sent on behalf of [payer name], who has partnered with [Optum or Optum Financial] to provide a faster, more efficient way to reimburse your business for services rendered.\nTo view the payment balance and all activity, please visit: https://payments.optum.com");
		remarks.setCustomRemarks2(
				"1. To view payment balance and activity, visit https://payments.optum.com. Call [payer phone] for support.");

		SubAccount subAccount = getSubAccount();
		subAccount.setName("testVCNonFun");
		subAccount.setReturnMailingAddress(setAddress());
		subAccount.setPhoneNumber("1234567890");
		subAccount.setClaimOfficePhoneNumber("1234567890");

		CardDetailsDTO cardDetailsDTO = new CardDetailsDTO();
		cardDetailsDTO.setMaskedPan("532426-xxxxxx-8701");
		cardDetailsDTO.setExpiryMMYY("1130");
		BinRange binRange = new BinRange();
		binRange.setBin("532426");
		cardDetailsDTO.setBinRange(binRange);

		PaymentRequest paymentRequest = getMockPaymentRequest();
		paymentRequest.setCorrelationId("corr-1");
		paymentRequest.getPaymentDetails().setIssuerPaymentId("issuer-1");
		paymentRequest.getPaymentDetails().setPin("1234");
		paymentRequest.getPaymentDetails().setId("pay-1");

		ReflectionTestUtils.setField(this.documentGenerationService, "templateFileName", tempTemplate.toString());
		ReflectionTestUtils.setField(this.documentGenerationService, "documentGenerationServiceUrl",
				"http://localhost/mock-doc");
		ReflectionTestUtils.setField(this.documentGenerationService, "imagePath", "missing-images");

		Mockito
			.when(this.paymentRequestRepository.findDocDetailsByIssuerPaymentIdAndCorrelationId(anyString(),
					anyString()))
			.thenReturn(summaryDocumentInputDto);
		Mockito
			.when(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(anyString(),
					anyString(), anyString()))
			.thenReturn(Optional.of(subAccount));
		Mockito
			.when(this.documentCustomRemarksRepository
				.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode(anyString(), anyString(), anyString()))
			.thenReturn(remarks);
		Mockito
			.when(this.logoDetailsRepository.findLogoUrlByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(
					anyString(), anyString(), anyString()))
			.thenReturn("https://logo");
		Mockito.when(this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.MASTERCARD.name()))
			.thenReturn("https://mc-logo");
		Mockito.when(this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.OPTUM.name()))
			.thenReturn("https://optum-logo");
		Mockito.when(this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.INSTRUCTIONS.name()))
			.thenReturn("https://instructions-logo");
		Mockito.when(this.barCodeService.generateBarCode(any())).thenReturn("barcode-url");
		Mockito.when(this.zetaApiService.getCardDetails(any())).thenReturn(cardDetailsDTO);
		AtomicReference<HttpHeaders> capturedHeaders = new AtomicReference<>();
		Mockito.when(this.documentGenerationClient.execute(any())).thenAnswer((invocation) -> {
			@SuppressWarnings("unchecked")
			Function<RestClient, ResponseEntity<String>> callback = invocation.getArgument(0);
			return callback.apply(this.restClient);
		});
		Mockito.when(this.restClient.post()).thenReturn(this.requestBodyUriSpec);
		Mockito.when(this.requestBodyUriSpec.uri(anyString())).thenReturn(this.requestBodyUriSpec);
		Mockito.when(this.requestBodyUriSpec.headers(any())).thenAnswer((invocation) -> {
			@SuppressWarnings("unchecked")
			java.util.function.Consumer<HttpHeaders> headersConsumer = invocation.getArgument(0);
			HttpHeaders headers = new HttpHeaders();
			headersConsumer.accept(headers);
			capturedHeaders.set(headers);
			return this.requestBodyUriSpec;
		});
		Mockito.doReturn(this.requestBodyUriSpec).when(this.requestBodyUriSpec).body(Mockito.any(MultiValueMap.class));
		Mockito.when(this.requestBodyUriSpec.retrieve()).thenReturn(this.responseSpec);
		Mockito.when(this.responseSpec.toEntity(String.class))
			.thenReturn(new ResponseEntity<>(
					"{\"documentId\":\"doc-1\",\"documentUrl\":\"https://doc\",\"metadata\":{},\"documentType\":\"pdf\"}",
					HttpStatus.OK));

		this.documentGenerationService.processSummaryDocumentMessage(paymentRequest, paymentExpiryDate,
				UUID.randomUUID());

		ArgumentCaptor<HttpEntity> requestCaptor = ArgumentCaptor.forClass(HttpEntity.class);
		Mockito.verify(this.documentGenerationClient).execute(any());
		ArgumentCaptor<EventBusEvent> cdeEventCaptor = ArgumentCaptor.forClass(EventBusEvent.class);
		Mockito.verify(this.eventBusProducer).sendEvent(cdeEventCaptor.capture(), Mockito.isNull());
		ArgumentCaptor<Object> bodyCaptor = ArgumentCaptor.forClass(Object.class);
		Mockito.verify(this.requestBodyUriSpec).body(bodyCaptor.capture());
		MultiValueMap<String, Object> body = (MultiValueMap<String, Object>) bodyCaptor.getValue();
		DocumentRequest outgoingDocumentRequest = (DocumentRequest) body.getFirst("incomingData");
		TemplateMappedValues values = (TemplateMappedValues) outgoingDocumentRequest.getTemplateMappedValues();
		DocumentRequestCDE cdePayload = (DocumentRequestCDE) cdeEventCaptor.getValue().getPayload();

		Assertions.assertThat(outgoingDocumentRequest).isNotNull();
		Assertions.assertThat(outgoingDocumentRequest.getDocType()).isEqualTo("summary-page");
		Assertions.assertThat(outgoingDocumentRequest.getIsPCI()).isFalse();
		Assertions.assertThat(cdePayload.getDocumentRequest().getIsPCI()).isTrue();
		Assertions.assertThat(values.getCustomRemarks1())
			.contains("<br/>")
			.contains("testVCNonFun")
			.contains("<a href=\"https://payments.optum.com\" target=\"_blank\">https://payments.optum.com</a>")
			.doesNotContain("[payer name]")
			.doesNotContain("\n");
		Assertions.assertThat(values.getCustomRemarks2()).hasSize(1);
		Assertions.assertThat(values.getCustomRemarks2().get(0))
			.contains("To view payment balance and activity, visit")
			.contains("<a href=\"https://payments.optum.com.\" target=\"_blank\">https://payments.optum.com.</a>")
			.contains("Call 1234567890 for support.")
			.doesNotContain("[payer phone]");
		Assertions.assertThat(values.getDate()).isEqualTo("02/03/2026");
		Assertions.assertThat(values.getExpiryDate()).isEqualTo("11/09/2026");
	}

}
