package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.BlobServiceClient;
import com.azure.storage.blob.specialized.BlockBlobClient;
import com.azure.storage.blob.specialized.BlobOutputStream;
import com.uhg.optum.financials.payments.payer.constant.PaymentDistributionConstants;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.document.TemplateMappedValues;
import com.uhg.optum.financials.payments.payer.model.Address;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.DistributionBatch;
import com.uhg.optum.financials.payments.payer.model.DistributionRecord;
import com.uhg.optum.financials.payments.payer.model.PayeeDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.DistributionBatchRepository;
import com.uhg.optum.financials.payments.payer.repository.DistributionRecordRepository;
import com.uhg.optum.financials.payments.payer.repository.DocumentCustomRemarksRepository;
import com.uhg.optum.financials.payments.payer.repository.LogoDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.BarCodeService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.util.ReflectionTestUtils;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.IOException;
import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.Base64;
import java.util.Collections;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.ThreadPoolExecutor;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicReference;
import java.util.zip.ZipOutputStream;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.lenient;
import static org.mockito.Mockito.atLeastOnce;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class VirtualCardDistributionServiceImplTests {

	@Mock
	private DistributionBatchRepository distributionBatchRepository;

	@Mock
	private DistributionRecordRepository distributionRecordRepository;

	@Mock
	private LogoDetailsRepository logoDetailsRepository;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private VirtualCardsRepository virtualCardsRepository;

	@Mock
	private CardCreationRepository cardCreationRepository;

	@Mock
	private SubAccountRepository subAccountRepository;

	@Mock
	private DocumentCustomRemarksRepository documentCustomRemarksRepository;

	@Mock
	private BarCodeService barCodeService;

	@Mock
	private BlobServiceClient blobServiceClient;

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private BlobClient blobClient;

	@Mock
	private BlockBlobClient blockBlobClient;

	@Mock
	private BlobOutputStream blobOutputStream;

	@InjectMocks
	private VirtualCardDistributionServiceImpl virtualCardDistributionServiceImpl;

	private ExecutorService htmlGenerationExecutor;

	@BeforeEach
	void setUp() {
		this.htmlGenerationExecutor = Executors.newSingleThreadExecutor();

		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "blobContainerName", "virtual-card");
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "cachedTemplate",
				"<html><body th:text=\"${paymentMdl.paymentIdentifier}\">fallback</body></html>");
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "htmlGenerationBatchSize", 10);
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "htmlGenerationExecutor",
				this.htmlGenerationExecutor);

		lenient().when(this.blobServiceClient.getBlobContainerClient("virtual-card"))
			.thenReturn(this.blobContainerClient);
		lenient().when(this.blobContainerClient.getBlobClient(anyString())).thenReturn(this.blobClient);
		lenient().when(this.blobClient.getBlockBlobClient()).thenReturn(this.blockBlobClient);
		lenient().when(this.blockBlobClient.getBlobOutputStream(true)).thenReturn(this.blobOutputStream);

		lenient().when(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(anyString()))
			.thenReturn(Optional.empty());
		lenient()
			.when(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(anyString(),
					anyString(), anyString()))
			.thenReturn(Optional.empty());
		lenient().when(this.documentCustomRemarksRepository.findByCustomRemarks1NotNull())
			.thenReturn(Collections.emptyList());
		lenient()
			.when(this.logoDetailsRepository.findLogoUrlByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(
					anyString(), anyString(), anyString()))
			.thenReturn(null);
		lenient()
			.when(this.logoDetailsRepository
				.findLogoUrlByAccountShortCodeAndSubAccountShortCodeIsNullAndOrgShortCode(anyString(), anyString()))
			.thenReturn(null);
		lenient()
			.when(this.logoDetailsRepository
				.findLogoUrlByAccountShortCodeIsNullAndSubAccountShortCodeIsNullAndOrgShortCode(anyString()))
			.thenReturn(null);
		lenient().when(this.virtualCardsRepository.findCardGuidByPaymentId(anyString())).thenReturn(UUID.randomUUID());
		lenient().when(this.barCodeService.generateBarCode(any())).thenReturn("https://example.com/barcode.png");
	}

	@AfterEach
	void tearDown() {
		ExecutorService executor = (ExecutorService) ReflectionTestUtils
			.getField(this.virtualCardDistributionServiceImpl, "htmlGenerationExecutor");
		if (executor != null) {
			executor.shutdownNow();
		}
		if (this.htmlGenerationExecutor != null && this.htmlGenerationExecutor != executor) {
			this.htmlGenerationExecutor.shutdownNow();
		}
	}

	@Test
	void initializeHtmlGenerationExecutor_createsConfiguredThreadPool() throws Exception {
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "htmlGenerationCoreThreads", 3);
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "htmlGenerationBatchSize", 2);
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "htmlGenerationExecutor", null);

		ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl, "initializeHtmlGenerationExecutor");

		ExecutorService executor = (ExecutorService) ReflectionTestUtils
			.getField(this.virtualCardDistributionServiceImpl, "htmlGenerationExecutor");
		Assertions.assertThat(executor).isInstanceOf(ThreadPoolExecutor.class);

		ThreadPoolExecutor threadPoolExecutor = (ThreadPoolExecutor) executor;
		Assertions.assertThat(threadPoolExecutor.getCorePoolSize()).isEqualTo(3);
		Assertions.assertThat(threadPoolExecutor.getMaximumPoolSize()).isEqualTo(3);
		Assertions.assertThat(threadPoolExecutor.getQueue().remainingCapacity()).isEqualTo(12);

		String threadName = threadPoolExecutor.submit(() -> Thread.currentThread().getName()).get(2, TimeUnit.SECONDS);
		Boolean daemonFlag = threadPoolExecutor.submit(() -> Thread.currentThread().isDaemon())
			.get(2, TimeUnit.SECONDS);
		Assertions.assertThat(threadName).startsWith("html-gen-");
		Assertions.assertThat(daemonFlag).isFalse();
	}

	@Test
	void initializeConstantCompanyLogos_populatesLogoCacheForSharedAssets() {
		@SuppressWarnings("unchecked")
		Map<String, String> logoCache = (Map<String, String>) ReflectionTestUtils
			.getField(this.virtualCardDistributionServiceImpl, "logoCache");
		logoCache.clear();

		ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl, "initializeConstantCompanyLogos");

		Assertions.assertThat(logoCache)
			.containsKeys(PaymentDistributionConstants.OPTUM_LOGO_RESOURCE,
					PaymentDistributionConstants.MASTERCARD_LOGO_RESOURCE);
		Assertions.assertThat(logoCache.get(PaymentDistributionConstants.OPTUM_LOGO_RESOURCE))
			.startsWith("data:image/png;base64,");
		Assertions.assertThat(logoCache.get(PaymentDistributionConstants.MASTERCARD_LOGO_RESOURCE))
			.startsWith("data:image/png;base64,");
	}

	@Test
	void processCorrelationBatchEvent_marksBatchFailedWhenNoPaymentRequestsFound() {
		AtomicReference<DistributionBatch> savedBatchRef = configureBatchRepository();
		CorrelationDto correlationDto = new CorrelationDto();
		correlationDto.setCorrelationId("corr-no-payments");

		BDDMockito.when(this.paymentRequestRepository.findByCorrelationId("corr-no-payments"))
			.thenReturn(Collections.emptyList());

		this.virtualCardDistributionServiceImpl.processCorrelationBatchEvent(correlationDto);

		Assertions.assertThat(savedBatchRef.get()).isNotNull();
		Assertions.assertThat(savedBatchRef.get().getStatus())
			.isEqualTo(PaymentDistributionConstants.BATCH_STATUS_FAILED);
		Assertions.assertThat(savedBatchRef.get().getErrorMessage())
			.isEqualTo("No payment requests found for correlation ID: corr-no-payments");
		verify(this.distributionRecordRepository, never()).save(any(DistributionRecord.class));
		verify(this.blobClient, never()).getBlockBlobClient();
	}

	@Test
	void processCorrelationBatchEvent_marksBatchFailedWhenPaymentLookupThrows() {
		AtomicReference<DistributionBatch> savedBatchRef = configureBatchRepository();
		CorrelationDto correlationDto = new CorrelationDto();
		correlationDto.setCorrelationId("corr-error");

		BDDMockito.when(this.paymentRequestRepository.findByCorrelationId("corr-error"))
			.thenThrow(new RuntimeException("database unavailable"));

		this.virtualCardDistributionServiceImpl.processCorrelationBatchEvent(correlationDto);

		Assertions.assertThat(savedBatchRef.get()).isNotNull();
		Assertions.assertThat(savedBatchRef.get().getStatus())
			.isEqualTo(PaymentDistributionConstants.BATCH_STATUS_FAILED);
	}

	@Test
	void processCorrelationBatchEvent_writesZipAndChecksumAndMarksBatchSent() {
		AtomicReference<DistributionBatch> savedBatchRef = configureBatchRepository();
		CorrelationDto correlationDto = new CorrelationDto();
		correlationDto.setCorrelationId("corr-success");
		seedConstantLogos();

		BDDMockito.when(this.paymentRequestRepository.findByCorrelationId("corr-success"))
			.thenReturn(List.of(createPaymentRequest("corr-success", "payment-1")));
		BDDMockito.when(this.distributionRecordRepository.save(any(DistributionRecord.class)))
			.thenAnswer((invocation) -> {
				DistributionRecord record = invocation.getArgument(0);
				if (record.getId() == null) {
					record.setId(11L);
				}
				return record;
			});

		this.virtualCardDistributionServiceImpl.processCorrelationBatchEvent(correlationDto);

		Assertions.assertThat(savedBatchRef.get()).isNotNull();
		Assertions.assertThat(savedBatchRef.get().getStatus())
			.isEqualTo(PaymentDistributionConstants.BATCH_STATUS_SENT);
		Assertions.assertThat(savedBatchRef.get().getZipFileName()).endsWith(".zip");

		verify(this.distributionRecordRepository, atLeastOnce()).save(any(DistributionRecord.class));
		verify(this.blockBlobClient).getBlobOutputStream(true);
		verify(this.blobClient, atLeastOnce()).upload(any(), anyLong(), eq(true));

		ArgumentCaptor<String> blobPathCaptor = ArgumentCaptor.forClass(String.class);
		verify(this.blobContainerClient, atLeastOnce()).getBlobClient(blobPathCaptor.capture());
		Assertions.assertThat(blobPathCaptor.getAllValues())
			.anyMatch((path) -> path.startsWith(PaymentDistributionConstants.BLOB_STORAGE_DISTRIBUTION_FOLDER + "/")
					&& path.endsWith(".sha256"));
	}

	@Test
	void setPaymentAmountsAndDates_formatsDateFieldsAsMmDdYyyy() {
		PaymentRequest paymentRequest = createPaymentRequest("corr-success", "payment-date-format");
		CardCreation cardCreation = new CardCreation();
		cardCreation.setExpiryDate(LocalDate.of(2026, 12, 5));
		TemplateMappedValues values = new TemplateMappedValues();

		BDDMockito.when(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc("payment-date-format"))
			.thenReturn(Optional.of(cardCreation));

		ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl, "setPaymentAmountsAndDates", values,
				paymentRequest);

		Assertions.assertThat(values.getDate()).isEqualTo("04/08/2026");
		Assertions.assertThat(values.getExpiryDate()).isEqualTo("12/05/2026");
	}

	@Test
	void buildSubAccountDto_returnsMappedSubAccountWhenRepositoryFindsRecord() {
		com.uhg.optum.financials.payments.organization.models.Address orgAddress = new com.uhg.optum.financials.payments.organization.models.Address();
		orgAddress.setAddressLine1("1 Test Way");
		orgAddress.setAddressLine2("Floor 2");
		orgAddress.setCity("Minneapolis");
		orgAddress.setState("MN");
		orgAddress.setPostalCode("55401");

		com.uhg.optum.financials.payments.payer.model.SubAccount subAccount = new com.uhg.optum.financials.payments.payer.model.SubAccount();
		subAccount.setName("Test Payer");
		subAccount.setPhoneNumber("5551112222");
		com.uhg.optum.financials.payments.payer.model.Address address = new com.uhg.optum.financials.payments.payer.model.Address();
		address.setLine1(orgAddress.getAddressLine1());
		address.setLine2(orgAddress.getAddressLine2());
		address.setCity(orgAddress.getCity());
		address.setState(orgAddress.getState());
		address.setPostalCode(orgAddress.getPostalCode());
		subAccount.setAddress(address);

		BDDMockito
			.when(this.subAccountRepository.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode("ACCOUNT1",
					"SUB1", "ORGCODE"))
			.thenReturn(Optional.of(subAccount));

		Object result = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl, "buildSubAccountDto",
				"ORGCODE", "ACCOUNT1", "SUB1");

		Assertions.assertThat(result)
			.isInstanceOf(com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto.class);
		com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto dto = (com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto) result;
		Assertions.assertThat(dto.getSubAccountRegistered()).isNotNull();
		Assertions.assertThat(dto.getSubAccountRegistered().getName()).isEqualTo("Test Payer");
		Assertions.assertThat(dto.getSubAccountRegistered().getPhoneNumber()).isEqualTo("5551112222");
	}

	@Test
	void templateLoadingAndLogoHelpers_coverClasspathFileAndResourcePaths() throws IOException {
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "cachedTemplate", null);
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "templateFileName",
				"does-not-matter.html");

		String templateFromClasspath = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"loadFromClasspath");
		Assertions.assertThat(templateFromClasspath).contains("<html");

		String resolvedTemplate = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"getOrLoadTemplate");
		Assertions.assertThat(resolvedTemplate).contains("<html");

		File tempTemplate = java.nio.file.Files.createTempFile("virtual-card-template", ".html").toFile();
		tempTemplate.deleteOnExit();
		java.nio.file.Files.writeString(tempTemplate.toPath(), "<html>from-file</html>");
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "templateFileName",
				tempTemplate.getAbsolutePath());
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "cachedTemplate", null);

		String templateFromFile = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"loadFromFile");
		Assertions.assertThat(templateFromFile).isEqualTo("<html>from-file</html>");

		String imageTypeJpeg = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"getImageTypeFromPath", "sample.jpg");
		String imageTypeGif = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"getImageTypeFromPath", "sample.gif");
		String imageTypeDefault = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"getImageTypeFromPath", "sample.png");
		Assertions.assertThat(imageTypeJpeg).isEqualTo(PaymentDistributionConstants.IMAGE_TYPE_JPEG);
		Assertions.assertThat(imageTypeGif).isEqualTo(PaymentDistributionConstants.IMAGE_TYPE_GIF);
		Assertions.assertThat(imageTypeDefault).isEqualTo(PaymentDistributionConstants.IMAGE_TYPE_PNG);

		String resourceLogo = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"loadLogoFromResources", PaymentDistributionConstants.OPTUM_LOGO_RESOURCE);
		Assertions.assertThat(resourceLogo).startsWith("data:image/png;base64,");

		AtomicReference<String> loadedLogo = new AtomicReference<>();
		ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl, "loadAndSetLogo",
				PaymentDistributionConstants.OPTUM_LOGO_RESOURCE, (java.util.function.Consumer<String>) loadedLogo::set,
				"Optum logo");
		Assertions.assertThat(loadedLogo.get()).startsWith("data:image/png;base64,");
	}

	@Test
	void downloadLogoBytes_readsFromFileUrl() throws IOException {
		File tempLogo = java.nio.file.Files.createTempFile("logo", ".png").toFile();
		tempLogo.deleteOnExit();
		byte[] expectedBytes = "logo-bytes".getBytes();
		java.nio.file.Files.write(tempLogo.toPath(), expectedBytes);

		byte[] actualBytes = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"downloadLogoBytes", tempLogo.toURI().toURL().toString());

		Assertions.assertThat(actualBytes).isEqualTo(expectedBytes);
	}

	@Test
	void stagingAndZipLogoHelpers_writeExpectedArtifacts() throws Exception {
		ReflectionTestUtils.setField(this.virtualCardDistributionServiceImpl, "htmlGenerationStagingEnabled", true);
		byte[] logoBytes = "org-logo".getBytes();
		String dataUriLogo = "data:image/png;base64," + Base64.getEncoder().encodeToString(logoBytes);

		ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl, "writeHtmlToStagingArea",
				"<html>content</html>", "payment-1.html", "corr-stage");
		ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl, "writeLogoToStagingArea", logoBytes,
				"hash1234", "corr-stage");
		ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl, "stageOrgLogoToBlobIfEnabled",
				logoBytes, "hash1234", "corr-stage");

		ByteArrayOutputStream baos = new ByteArrayOutputStream();
		ZipOutputStream zos = new ZipOutputStream(baos);
		int constantLogoWrites = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"writeLogoToZipIfNeeded", dataUriLogo, "logos/constant.png", new java.util.HashSet<String>(), zos);
		Assertions.assertThat(constantLogoWrites).isEqualTo(1);

		com.uhg.optum.financials.payments.payer.dto.async.HtmlGenerationResult result = new com.uhg.optum.financials.payments.payer.dto.async.HtmlGenerationResult(
				"payment-1", "payment-1.html");
		result.setOrgLogoHash("hash1234");
		result.setOrgLogoSource(dataUriLogo);

		int orgLogoWrites = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"writeOrgLogoToZipIfNeeded", result, new java.util.HashSet<String>(), "corr-stage", zos);
		Assertions.assertThat(orgLogoWrites).isEqualTo(1);

		int orgLogoDirectWrites = ReflectionTestUtils.invokeMethod(this.virtualCardDistributionServiceImpl,
				"writeOrgLogoToZipAndStageIfNeeded", dataUriLogo, "hash9999", "logos/hash9999.png",
				new java.util.HashSet<String>(), "corr-stage", zos);
		Assertions.assertThat(orgLogoDirectWrites).isEqualTo(1);

		zos.finish();
		zos.close();

		verify(this.blobClient, atLeastOnce()).upload(any(), anyLong(), eq(true));
	}

	@SuppressWarnings("unchecked")
	private void seedConstantLogos() {
		Map<String, String> logoCache = (Map<String, String>) ReflectionTestUtils
			.getField(this.virtualCardDistributionServiceImpl, "logoCache");
		String logoDataUri = "data:image/png;base64," + Base64.getEncoder().encodeToString("logo".getBytes());
		logoCache.put(PaymentDistributionConstants.OPTUM_LOGO_RESOURCE, logoDataUri);
		logoCache.put(PaymentDistributionConstants.MASTERCARD_LOGO_RESOURCE, logoDataUri);
	}

	private AtomicReference<DistributionBatch> configureBatchRepository() {
		AtomicReference<DistributionBatch> savedBatchRef = new AtomicReference<>();

		BDDMockito.when(this.distributionBatchRepository.save(any(DistributionBatch.class)))
			.thenAnswer((invocation) -> {
				DistributionBatch batch = invocation.getArgument(0);
				if (batch.getId() == null) {
					batch.setId(1L);
				}
				if (batch.getCreatedAt() == null) {
					batch.setCreatedAt(LocalDateTime.now());
				}
				savedBatchRef.set(batch);
				return batch;
			});

		BDDMockito.when(this.distributionBatchRepository.findById(1L))
			.thenAnswer((invocation) -> Optional.ofNullable(savedBatchRef.get()));

		return savedBatchRef;
	}

	private PaymentRequest createPaymentRequest(String correlationId, String paymentId) {
		PaymentDetails paymentDetails = new PaymentDetails();
		paymentDetails.setId(paymentId);
		paymentDetails.setAmount(BigDecimal.valueOf(25.75));
		paymentDetails.setPaymentDate(LocalDate.of(2026, 4, 8));
		paymentDetails.setIssuerPaymentId("issuer-123");
		paymentDetails.setPin("1234");

		Address address = new Address();
		address.setLine1("123 Main St");
		address.setLine2("Suite 100");
		address.setCity("Eden Prairie");
		address.setState("MN");
		address.setPostalCode("55344");
		address.setCountry("US");

		PayeeDetails payeeDetails = new PayeeDetails();
		payeeDetails.setName("Provider One");
		payeeDetails.setAddress(address);

		PaymentRequest paymentRequest = new PaymentRequest();
		paymentRequest.setCorrelationId(correlationId);
		paymentRequest.setOrgShortCode("ORGCODE");
		paymentRequest.setAccountShortCode("ACCOUNT1");
		paymentRequest.setSubAccountShortCode("SUB1");
		paymentRequest.setPaymentDetails(paymentDetails);
		paymentRequest.setPayeeDetails(payeeDetails);
		paymentRequest.setStatus("NEW");
		return paymentRequest;
	}

}
