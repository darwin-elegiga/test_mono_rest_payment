package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobServiceClient;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.organization.models.Address;
import com.uhg.optum.financials.payments.payer.constant.PaymentDistributionConstants;
import com.uhg.optum.financials.payments.payer.dto.AcquirerAddress;
import com.uhg.optum.financials.payments.payer.dto.async.HtmlGenerationResult;
import com.uhg.optum.financials.payments.payer.dto.batch.CorrelationDto;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.payments.payer.dto.document.TemplateMappedValues;
import com.uhg.optum.financials.payments.payer.model.DistributionBatch;
import com.uhg.optum.financials.payments.payer.model.DistributionRecord;
import com.uhg.optum.financials.payments.payer.model.DocumentCustomRemarks;
import com.uhg.optum.financials.payments.payer.model.PayeeDetails;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.DistributionBatchRepository;
import com.uhg.optum.financials.payments.payer.repository.DistributionRecordRepository;
import com.uhg.optum.financials.payments.payer.repository.DocumentCustomRemarksRepository;
import com.uhg.optum.financials.payments.payer.repository.LogoDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.service.BarCodeService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardDistributionService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.thymeleaf.TemplateEngine;
import org.thymeleaf.context.Context;
import org.thymeleaf.templateresolver.StringTemplateResolver;

import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.security.DigestOutputStream;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.net.URL;
import java.net.URLConnection;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.Base64;
import java.util.Collections;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Future;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.ThreadFactory;
import java.util.concurrent.ThreadPoolExecutor;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.function.Consumer;
import java.util.zip.ZipEntry;
import java.util.zip.ZipOutputStream;
import jakarta.annotation.PostConstruct;
import jakarta.annotation.PreDestroy;
import com.uhg.optum.financials.payments.payer.util.HtmlGenerationResultWrapper;

@Service
@Slf4j
public class VirtualCardDistributionServiceImpl implements VirtualCardDistributionService {

	@Value("${document.template.file-name}")
	private String templateFileName;

	@Value("${storage.containerName}")
	private String blobContainerName;

	@Value("${html.generation.core-threads:4}")
	private int htmlGenerationCoreThreads;

	@Value("${html.generation.batch-size:50}")
	private int htmlGenerationBatchSize;

	@Value("${html.generation.staging.enabled:false}")
	private boolean htmlGenerationStagingEnabled;

	private final DistributionBatchRepository distributionBatchRepository;

	private final DistributionRecordRepository distributionRecordRepository;

	private final LogoDetailsRepository logoDetailsRepository;

	private final PaymentRequestRepository paymentRequestRepository;

	private final VirtualCardsRepository virtualCardsRepository;

	private final CardCreationRepository cardCreationRepository;

	private final SubAccountRepository subAccountRepository;

	private final DocumentCustomRemarksRepository documentCustomRemarksRepository;

	private final BarCodeService barCodeService;

	private final BlobServiceClient blobServiceClient;

	private final TemplateEngine stringTemplateEngine;

	private String cachedTemplate;

	private final Map<String, String> logoCache = new java.util.concurrent.ConcurrentHashMap<>();

	private ExecutorService htmlGenerationExecutor;

	public VirtualCardDistributionServiceImpl(DistributionBatchRepository distributionBatchRepository,
			DistributionRecordRepository distributionRecordRepository, LogoDetailsRepository logoDetailsRepository,
			PaymentRequestRepository paymentRequestRepository, VirtualCardsRepository virtualCardsRepository,
			CardCreationRepository cardCreationRepository, SubAccountRepository subAccountRepository,
			DocumentCustomRemarksRepository documentCustomRemarksRepository, BarCodeService barCodeService,
			BlobServiceClient blobServiceClient) {
		this.distributionBatchRepository = distributionBatchRepository;
		this.distributionRecordRepository = distributionRecordRepository;
		this.logoDetailsRepository = logoDetailsRepository;
		this.paymentRequestRepository = paymentRequestRepository;
		this.virtualCardsRepository = virtualCardsRepository;
		this.cardCreationRepository = cardCreationRepository;
		this.subAccountRepository = subAccountRepository;
		this.documentCustomRemarksRepository = documentCustomRemarksRepository;
		this.barCodeService = barCodeService;
		this.blobServiceClient = blobServiceClient;

		// Initialize a separate TemplateEngine for processing raw string templates
		this.stringTemplateEngine = new TemplateEngine();
		this.stringTemplateEngine.setTemplateResolver(new StringTemplateResolver());
	}

	@PostConstruct
	private void initializeResources() {
		initializeHtmlGenerationExecutor();
		initializeConstantCompanyLogos();
	}

	private void initializeHtmlGenerationExecutor() {
		int corePoolSize = Math.max(1, this.htmlGenerationCoreThreads);
		int queueCapacity = Math.max(corePoolSize * 4, Math.max(1, this.htmlGenerationBatchSize));

		this.htmlGenerationExecutor = new ThreadPoolExecutor(corePoolSize, corePoolSize, 60, TimeUnit.SECONDS,
				new LinkedBlockingQueue<>(queueCapacity), new ThreadFactory() {
					private final AtomicInteger threadNumber = new AtomicInteger(0);

					@Override
					public Thread newThread(Runnable r) {
						Thread t = new Thread(r, "html-gen-" + this.threadNumber.incrementAndGet());
						t.setDaemon(false);
						return t;
					}
				}, new ThreadPoolExecutor.CallerRunsPolicy());

		log.info("HTML generation thread pool initialized with threads={}, queueCapacity={}", corePoolSize,
				queueCapacity);
	}

	/**
	 * Initialize constant company logos (Optum and Mastercard) at startup. These logos
	 * are the same for all payments and should be loaded only once.
	 */
	private void initializeConstantCompanyLogos() {
		try {
			log.debug("Initializing constant company logos at startup");
			loadAndSetLogo(PaymentDistributionConstants.OPTUM_LOGO_RESOURCE,
					(logo) -> this.logoCache.putIfAbsent(PaymentDistributionConstants.OPTUM_LOGO_RESOURCE, logo),
					"Optum logo");
			loadAndSetLogo(PaymentDistributionConstants.MASTERCARD_LOGO_RESOURCE,
					(logo) -> this.logoCache.putIfAbsent(PaymentDistributionConstants.MASTERCARD_LOGO_RESOURCE, logo),
					"Mastercard logo");
			log.info("Constant company logos initialized successfully");
		}
		catch (Exception ex) {
			log.warn("Failed to initialize constant company logos: {}", ex.getMessage());
		}
	}

	@Override
	public void processCorrelationBatchEvent(CorrelationDto correlationDto) {
		DistributionBatch savedBatch = null;
		try {
			String correlationId = correlationDto.getCorrelationId();
			log.info("Processing correlation batch event for correlation ID: {}", correlationId);

			savedBatch = this.distributionBatchRepository.save(createDistributionBatch(correlationDto));
			log.info("Distribution batch created with ID: {} for correlation ID: {}", savedBatch.getId(),
					correlationId);

			// Fetch all payment requests for this correlation ID
			// raise alert if amount, number of payments doesn't matches
			List<PaymentRequest> paymentRequests = this.paymentRequestRepository.findByCorrelationId(correlationId);

			if ((paymentRequests == null) || paymentRequests.isEmpty()) {
				String errorMessage = "No payment requests found for correlation ID: " + correlationId;
				log.warn(errorMessage);
				updateBatchStatus(savedBatch, PaymentDistributionConstants.BATCH_STATUS_FAILED, errorMessage);
				return;
			}

			log.info("Found {} payment requests for correlation ID: {}", paymentRequests.size(), correlationId);

			processPaymentsAndCreateZip(savedBatch, paymentRequests, correlationId);

			// Update batch status to SENT (terminal success state) after ZIP transmission
			updateBatchStatus(savedBatch, PaymentDistributionConstants.BATCH_STATUS_SENT, null);
			log.info(
					"Correlation batch event processing completed successfully. Batch status set to SENT for correlation ID: {}",
					correlationId);
		}
		catch (Exception ex) {
			log.error("Error occurred while processing correlation batch event: {} - {}", ex.getClass().getName(),
					ex.getMessage(), ex);
			if (savedBatch != null) {
				updateBatchStatus(savedBatch, PaymentDistributionConstants.BATCH_STATUS_FAILED, null);
				log.error("Batch status set to FAILED for correlation ID: {} due to error",
						savedBatch.getCorrelationId());
			}
		}
	}

	private void processPaymentsAndCreateZip(DistributionBatch batch, List<PaymentRequest> paymentRequests,
			String correlationId) throws IOException {
		String orgShortCode = paymentRequests.get(0).getOrgShortCode();
		String zipFileName = generateZipFileName(orgShortCode, correlationId, batch.getCreatedAt());
		int batchSize = Math.max(1, this.htmlGenerationBatchSize);

		try {
			log.info("Starting async HTML generation for {} payments under correlation ID: {}", paymentRequests.size(),
					correlationId);

			// Pre-process: Set company logos once (sync) before async generation
			// Company logos (Optum, Mastercard) are the same for all payments
			log.debug("Setting company logos once for all payments");
			TemplateMappedValues companyLogosTemplate = new TemplateMappedValues();
			setCompanyLogosFromCache(companyLogosTemplate);
			String optumLogo = companyLogosTemplate.getOptumLogo();
			String mastercardLogo = companyLogosTemplate.getCardTypeLogo();
			log.debug("Company logos prepared: optumLogo set={}, mastercardLogo set={}",
					optumLogo != null && !optumLogo.isEmpty(), mastercardLogo != null && !mastercardLogo.isEmpty());

			long totalStartTime = System.currentTimeMillis();
			writeZipToBlobStorage(zipFileName, batch.getId(), paymentRequests, correlationId, batch.getCreatedAt(),
					optumLogo, mastercardLogo, batchSize);
			long totalEndTime = System.currentTimeMillis();

			batch.setZipFileName(zipFileName);
			batch.setStatus(PaymentDistributionConstants.BATCH_STATUS_ZIP_GENERATED);
			batch.setUpdatedAt(LocalDateTime.now());
			this.distributionBatchRepository.save(batch);
			log.info(
					"Batch status updated to ZIP_GENERATED for correlation ID: {}. Total processing time: {}ms with batchSize={}",
					correlationId, totalEndTime - totalStartTime, batchSize);
		}
		catch (IOException ex) {
			log.error("IO error while creating and streaming ZIP to blob storage for correlation ID: {}. Error: {}",
					correlationId, ex.getMessage(), ex);
			throw ex;
		}
		catch (Exception ex) {
			log.error("Error while creating and streaming ZIP to blob storage for correlation ID: {}. Error: {}",
					correlationId, ex.getMessage(), ex);
			throw new IOException("Failed to create and stream ZIP to blob storage: " + ex.getMessage(), ex);
		}
	}

	/**
	 * Generates HTML for all payments in parallel using thread pool. This is
	 * CPU-intensive work that can be parallelized independently for each payment.
	 *
	 * Phase 1 of the async processing (PARALLEL): - Creates distribution records -
	 * Generates HTML content - Collects logos (no ZIP writing yet) - Generates
	 * supplemental records
	 *
	 * Phase 2 (ZIP writing) remains single-threaded for safety.
	 */
	private List<HtmlGenerationResult> generateHtmlAsyncParallel(Long batchId, List<PaymentRequest> paymentRequests,
			String optumLogo, String mastercardLogo) {

		List<Future<HtmlGenerationResult>> futures = new ArrayList<>();
		List<HtmlGenerationResult> results = new ArrayList<>();

		// Submit all HTML generation tasks to thread pool
		for (PaymentRequest paymentRequest : paymentRequests) {
			Future<HtmlGenerationResult> future = this.htmlGenerationExecutor.submit(() -> {
				long taskStartTime = System.currentTimeMillis();
				try {
					String paymentId = extractPaymentId(paymentRequest);
					UUID cardGuid = this.virtualCardsRepository.findCardGuidByPaymentId(paymentId);

					DistributionRecord record = new DistributionRecord();
					record.setDistributionBatchId(batchId);
					record.setPaymentId(paymentId);
					// to be removed later
					record.setCardGuid((cardGuid != null) ? cardGuid.toString() : UUID.randomUUID().toString());
					record.setHtmlFileName(paymentId + PaymentDistributionConstants.HTML_EXTENSION);
					record.setDistributionMethod(PaymentDistributionConstants.DEFAULT_DISTRIBUTION_METHOD);
					record.setStatus(PaymentDistributionConstants.RECORD_STATUS_PENDING);
					record.setCreatedAt(LocalDateTime.now());

					DistributionRecord savedRecord = this.distributionRecordRepository.save(record);
					log.debug("Created distribution record for payment ID: {} with status PENDING", paymentId);

					String templateContent = getOrLoadTemplate();
					HtmlGenerationResultWrapper generatedHtml = populateTemplateWithValues(templateContent,
							paymentRequest, optumLogo, mastercardLogo);
					String htmlContent = generatedHtml.getHtmlContent();
					TemplateMappedValues values = generatedHtml.getTemplateMappedValues();

					HtmlGenerationResult result = new HtmlGenerationResult(paymentId, record.getHtmlFileName());
					result.setHtmlContent(htmlContent);

					// Store only lightweight metadata during async processing.
					collectLogosForResult(values, result);

					String paymentLogoUrl = values.getLogoUrl();
					String supplementalRecord = buildSupplementalRecord(paymentRequest, savedRecord, paymentId,
							paymentLogoUrl);
					if (supplementalRecord != null && !supplementalRecord.isEmpty()) {
						result.setSupplementalRecord(supplementalRecord);
					}

					// Update distribution record status
					savedRecord.setStatus(PaymentDistributionConstants.RECORD_STATUS_HTML_CREATED);
					savedRecord.setUpdatedAt(LocalDateTime.now());
					this.distributionRecordRepository.save(savedRecord);

					result.setSuccess(true);
					result.setGenerationTimeMs(System.currentTimeMillis() - taskStartTime);

					log.debug("HTML generation completed for payment ID: {} in {} ms", paymentId,
							result.getGenerationTimeMs());
					return result;
				}
				catch (Exception ex) {
					log.error("Error during HTML generation for payment ID: {}. Error: {}",
							extractPaymentId(paymentRequest), ex.getMessage(), ex);

					HtmlGenerationResult errorResult = new HtmlGenerationResult(extractPaymentId(paymentRequest), "");
					errorResult.setSuccess(false);
					errorResult.setErrorMessage(ex.getMessage());
					errorResult.setGenerationTimeMs(System.currentTimeMillis() - taskStartTime);
					return errorResult;
				}
			});
			futures.add(future);
		}

		// Collect results from all futures - common thread
		for (int i = 0; i < futures.size(); i++) {
			try {
				HtmlGenerationResult result = futures.get(i).get();
				results.add(result);
				log.debug("Result {} collected: paymentId={}, success={}", i + 1, result.getPaymentId(),
						result.isSuccess());
			}
			catch (Exception ex) {
				log.error("Error retrieving result for task {}. Error: {}", i + 1, ex.getMessage(), ex);
				HtmlGenerationResult errorResult = new HtmlGenerationResult(null, "");
				errorResult.setSuccess(false);
				errorResult.setErrorMessage(ex.getMessage());
				results.add(errorResult);
			}
		}

		log.info("Async HTML generation completed. Total: {}, Success: {}", results.size(),
				results.stream().filter(HtmlGenerationResult::isSuccess).count());
		return results;
	}

	/**
	 * Collects lightweight logo metadata during async processing so worker threads stay
	 * focused on template generation instead of network I/O.
	 */
	private void collectLogosForResult(TemplateMappedValues values, HtmlGenerationResult result) {
		try {
			log.debug("Collecting logos for payment ID: {}", values.getPaymentIdentifier());
			collectOrgLogo(values, result);
		}
		catch (Exception ex) {
			log.warn("Error collecting logos for result. Error: {}", ex.getMessage());
		}
	}

	/**
	 * Collects organization logo metadata so the ZIP phase can download and write each
	 * unique logo only once.
	 */
	private void collectOrgLogo(TemplateMappedValues values, HtmlGenerationResult result) {
		if (!isValidLogoData(values.getLogoUrl())) {
			return;
		}

		String logoUrlHash = String.format("%08x", values.getLogoUrl().hashCode());
		result.setOrgLogoHash(logoUrlHash);
		result.setOrgLogoSource(values.getLogoUrl());
	}

	/**
	 * Validates if logo data is present and not empty.
	 */
	private boolean isValidLogoData(String logoData) {
		return logoData != null && !logoData.isEmpty();
	}

	// ...existing code...

	/**
	 * Extracts logo bytes from data URI (base64 encoded).
	 */
	private byte[] extractLogoBytesFromDataUri(String dataUri) {
		try {
			if (dataUri.startsWith("data:image")) {
				String base64Content = dataUri.substring(dataUri.indexOf(",") + 1);
				return Base64.getDecoder().decode(base64Content);
			}
		}
		catch (Exception ex) {
			log.warn("Error extracting logo bytes from data URI. Error: {}", ex.getMessage());
		}
		return null;
	}

	/**
	 * Downloads logo bytes from URL.
	 */
	private byte[] downloadLogoBytes(String logoUrl) throws IOException {
		try {
			URL url = new URL(logoUrl);
			URLConnection connection = url.openConnection();
			connection.setConnectTimeout(10000);
			connection.setReadTimeout(10000);

			try (InputStream inputStream = connection.getInputStream()) {
				return inputStream.readAllBytes();
			}
		}
		catch (IOException ex) {
			log.error("Error downloading logo from URL {}: {}", logoUrl, ex.getMessage());
			throw new IOException("Failed to download logo from URL: " + logoUrl, ex);
		}
	}

	/**
	 * Update batch status and optionally set error message.
	 * @param batch the distribution batch to update
	 * @param status the status to set
	 * @param errorMessage the error message to set (can be null)
	 */
	private void updateBatchStatus(DistributionBatch batch, String status, String errorMessage) {
		DistributionBatch updatedBatch = this.distributionBatchRepository.findById(batch.getId()).orElse(batch);
		updatedBatch.setStatus(status);
		if (errorMessage != null) {
			updatedBatch.setErrorMessage(errorMessage);
		}
		updatedBatch.setUpdatedAt(LocalDateTime.now());
		this.distributionBatchRepository.save(updatedBatch);
	}

	private DistributionBatch createDistributionBatch(CorrelationDto correlationDto) {
		DistributionBatch batch = new DistributionBatch();
		batch.setCorrelationId(correlationDto.getCorrelationId());
		batch.setZipFileName("");
		batch.setStatus(PaymentDistributionConstants.BATCH_STATUS_PENDING);
		batch.setRetryCount(0);
		batch.setCreatedAt(LocalDateTime.now());
		return batch;
	}

	private String extractPaymentId(PaymentRequest paymentRequest) {
		if (paymentRequest.getPaymentDetails() != null && paymentRequest.getPaymentDetails().getId() != null) {
			return paymentRequest.getPaymentDetails().getId();
		}
		return null;
	}

	private String getOrLoadTemplate() throws IOException {
		if (this.cachedTemplate != null) {
			return this.cachedTemplate;
		}

		try {
			log.debug("Loading template from classpath resources");

			String templateContent = loadFromClasspath();
			if (templateContent != null) {
				log.info("Template loaded successfully from classpath resources");
				this.cachedTemplate = templateContent;
				return this.cachedTemplate;
			}

			log.debug("Template not found in classpath, trying configured path: {}", this.templateFileName);
			templateContent = loadFromFile();
			if (templateContent != null) {
				log.info("Template loaded successfully from configured path: {}", this.templateFileName);
				this.cachedTemplate = templateContent;
				return this.cachedTemplate;
			}

			log.error("Template file not found in classpath or at configured path: {}", this.templateFileName);
			throw new IOException("Template file 'virtual_card_template.html' not found in classpath or at: "
					+ this.templateFileName);
		}
		catch (IOException ex) {
			log.error("IO error loading template file: {}", ex.getMessage(), ex);
			throw ex;
		}
	}

	private String loadFromClasspath() {
		try (InputStream inputStream = getClass().getClassLoader().getResourceAsStream("virtual_card_template.html")) {
			if (inputStream != null) {
				log.debug("Found template in classpath resources");
				return new String(inputStream.readAllBytes(), StandardCharsets.UTF_8);
			}
		}
		catch (IOException ex) {
			log.debug("Failed to load template from classpath: {}", ex.getMessage());
		}
		return null;
	}

	private String loadFromFile() {
		File templateFile = new File(this.templateFileName);
		if (templateFile.exists()) {
			try {
				return Files.readString(templateFile.toPath(), StandardCharsets.UTF_8);
			}
			catch (IOException ex) {
				log.warn("Failed to read template file: {}", ex.getMessage());
			}
		}
		return null;
	}

	/**
	 * Populates template and returns both HTML content and TemplateMappedValues. This
	 * method should be used when the TemplateMappedValues object needs to be reused
	 * (e.g., for logo collection) to avoid redundant buildTemplateMappedValues calls.
	 */
	private HtmlGenerationResultWrapper populateTemplateWithValues(String templateContent,
			PaymentRequest paymentRequest, String optumLogo, String mastercardLogo) {
		try {
			TemplateMappedValues templateMappedValues = buildTemplateMappedValues(paymentRequest);

			// Set company logos that was pre-loaded from cache once for all payments.
			// These logos are constant and do not need to be reloaded for each payment.
			templateMappedValues.setOptumLogo(optumLogo);
			templateMappedValues.setCardTypeLogo(mastercardLogo);

			Context context = new Context();
			context.setVariable("paymentMdl", templateMappedValues);

			String htmlContent = this.stringTemplateEngine.process(templateContent, context);

			log.debug("Template populated successfully with values for payment ID: {}",
					extractPaymentId(paymentRequest));
			return new HtmlGenerationResultWrapper(htmlContent, templateMappedValues);
		}
		catch (Exception ex) {
			log.error("Error populating template with values: {}", ex.getMessage());
			if (ex.getCause() != null) {
				log.error("Root cause: {}", ex.getCause().getMessage());
			}
			throw new RuntimeException("Failed to populate template with values: " + ex.getMessage(), ex);
		}
	}

	private TemplateMappedValues buildTemplateMappedValues(PaymentRequest paymentRequest) {
		try {
			log.debug("Building template mapped values for payment ID: {}", extractPaymentId(paymentRequest));
			TemplateMappedValues values = new TemplateMappedValues();
			String paymentId = extractPaymentId(paymentRequest);
			String orgName = (paymentRequest.getOrgShortCode() != null) ? paymentRequest.getOrgShortCode()
					: "ABC Medical Center";

			values.setPaymentIdentifier(paymentId);
			values.setPaymentType(PaymentDistributionConstants.PAYMENT_TYPE_VIRTUAL_CARD);
			values.setPayeeDetails(paymentRequest.getPayeeDetails());

			setPaymentAmountsAndDates(values, paymentRequest);

			SubAccountDto subAccountDto = buildSubAccountDto(orgName, paymentRequest.getAccountShortCode(),
					paymentRequest.getSubAccountShortCode());
			values.setSubAccountDto(subAccountDto);
			if (subAccountDto.getSubAccountRegistered() != null) {
				values.setPayerName(subAccountDto.getSubAccountRegistered().getName());
			}

			populateCustomRemarks(values);
			setOrgLogosAndCardDetails(values, orgName, paymentRequest.getAccountShortCode(),
					paymentRequest.getSubAccountShortCode());

			setBarCodeUrl(values, paymentRequest, paymentId);

			return values;
		}
		catch (Exception ex) {
			log.error("Error building template mapped values: {}", ex.getMessage(), ex);
			throw new RuntimeException("Failed to build template mapped values: " + ex.getMessage(), ex);
		}
	}

	private void setPaymentAmountsAndDates(TemplateMappedValues values, PaymentRequest paymentRequest) {
		DateTimeFormatter templateDateFormatter = DateTimeFormatter.ofPattern("MM/dd/yyyy");
		if ((paymentRequest.getPaymentDetails() != null) && (paymentRequest.getPaymentDetails().getAmount() != null)) {
			values.setAmount(paymentRequest.getPaymentDetails().getAmount().toString());
		}
		else {
			values.setAmount("");
		}

		String expiryDate = this.cardCreationRepository
			.findTopByPaymentIdOrderByCreatedAtDesc(extractPaymentId(paymentRequest))
			.map((cc) -> (cc.getExpiryDate() != null) ? cc.getExpiryDate().format(templateDateFormatter) : "")
			.orElse("");
		values.setExpiryDate(expiryDate);

		if ((paymentRequest.getPaymentDetails() != null)
				&& (paymentRequest.getPaymentDetails().getPaymentDate() != null)) {
			values.setDate(paymentRequest.getPaymentDetails().getPaymentDate().format(templateDateFormatter));
		}
		else {
			values.setDate(LocalDate.now().format(templateDateFormatter));
		}

		String pin = (paymentRequest.getPaymentDetails() != null) ? paymentRequest.getPaymentDetails().getPin() : "";
		String issuerPaymentId = (paymentRequest.getPaymentDetails() != null)
				? paymentRequest.getPaymentDetails().getIssuerPaymentId() : "";
		String networkPaymentId = (paymentRequest.getPaymentDetails() != null)
				? paymentRequest.getPaymentDetails().getNetworkPaymentId() : "";
		values.setPin((pin != null) ? pin : "");
		values.setIssuerPaymentIdList((issuerPaymentId != null) ? issuerPaymentId : "");
		values.setNetworkPaymentId((networkPaymentId != null) ? networkPaymentId : "");
	}

	private void setBarCodeUrl(TemplateMappedValues values, PaymentRequest paymentRequest, String paymentId) {
		try {
			AcquirerAddress address = buildAcquirerAddress(paymentRequest.getPayeeDetails());
			String barCodeUrl = this.barCodeService.generateBarCode(address);
			values.setBarCodeUrl((barCodeUrl != null) ? barCodeUrl : "");
			log.debug("Barcode generated successfully for payment ID: {}", paymentId);
		}
		catch (Exception ex) {
			log.warn("Failed to generate barcode for payment ID: {}. Error: {}", paymentId, ex.getMessage());
			values.setBarCodeUrl("");
		}
	}

	private AcquirerAddress buildAcquirerAddress(PayeeDetails payeeDetails) {
		AcquirerAddress address = new AcquirerAddress();
		if (payeeDetails != null && payeeDetails.getAddress() != null) {
			com.uhg.optum.financials.payments.payer.model.Address addr = payeeDetails.getAddress();
			address.setLine1(addr.getLine1());
			address.setLine2(addr.getLine2());
			address.setCity(addr.getCity());
			address.setState(addr.getState());
			address.setZipCode(String.valueOf(addr.getPostalCode()));
			address.setCountry(addr.getCountry());
		}
		return address;
	}

	private SubAccountDto buildSubAccountDto(String orgName, String accountName, String subAccountName) {
		SubAccountDto subPayer = new SubAccountDto();
		Optional<SubAccount> subAccountOptional = this.subAccountRepository
			.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(accountName, subAccountName, orgName);

		if (subAccountOptional.isPresent()) {
			SubAccount subAccount = subAccountOptional.get();

			SubAccountRegistered subAccountRegistered = new SubAccountRegistered();
			subAccountRegistered.setName(subAccount.getName());

			Address address = new Address();
			address.setAddressLine1(subAccount.getAddress().getLine1());
			address.setAddressLine2(subAccount.getAddress().getLine2());
			address.setCity(subAccount.getAddress().getCity());
			address.setState(subAccount.getAddress().getState());
			address.setPostalCode(subAccount.getAddress().getPostalCode());
			subAccountRegistered.setAddress(address);
			subAccountRegistered.setPhoneNumber(subAccount.getPhoneNumber());
			subPayer.setSubAccountRegistered(subAccountRegistered);
		}
		return subPayer;
	}

	private void populateCustomRemarks(TemplateMappedValues values) {
		try {
			log.debug("Populating custom remarks for paymentId: {}", values.getPaymentIdentifier());
			com.uhg.optum.financials.payments.payer.model.DocumentCustomRemarks remarks = getCustomRemarks();
			if (remarks == null) {
				log.warn("Custom remarks not found, using default empty values");
				values.setCustomRemarks1("");
				values.setCustomRemarks2(Collections.singletonList(""));
			}
			else {
				values.setCustomRemarks1(remarks.getCustomRemarks1());
				values.setCustomRemarks2(Collections.singletonList(remarks.getCustomRemarks2()));
				log.debug("Custom remarks populated from database");
			}
			log.debug("Custom remarks populated successfully");
		}
		catch (Exception ex) {
			log.error("Error populating custom remarks: {}", ex.getMessage(), ex);
			values.setCustomRemarks1("");
			values.setCustomRemarks2(Collections.singletonList(""));
		}
	}

	private void setOrgLogosAndCardDetails(TemplateMappedValues values, String orgShortCode, String accountShortCode,
			String subAccountShortCode) {
		try {
			log.debug("Starting logo and card details setup for org: {}, account: {}, subAccount: {}, paymentId: {}",
					orgShortCode, accountShortCode, subAccountShortCode, values.getPaymentIdentifier());

			setOrgLogoOnly(values, orgShortCode, accountShortCode, subAccountShortCode);

			// Set default card details
			initializeCardDetails(values);

			log.debug("Logos and card details populated successfully for payment ID: {}",
					values.getPaymentIdentifier());
		}
		catch (Exception ex) {
			log.error("Error populating logos and card details [{}]: {}", ex.getClass().getSimpleName(),
					(ex.getMessage() != null) ? ex.getMessage() : ex.getClass().getSimpleName(), ex);
			// Ensure defaults are set even on error
			initializeCardDetails(values);
		}
		finally {
			// Log actual state of card details initialization
			boolean hasCardDetails = values.getCardLastFourDigits() != null && values.getCvv() != null;
			log.debug("Card details initialization completed for payment ID: {}, hasCardDetails={}",
					values.getPaymentIdentifier(), hasCardDetails);
		}
	}

	/**
	 * Set only organization-specific logo for a payment (called during async processing).
	 * Company logos (Optum, Mastercard) are set once during sync processing before async
	 * tasks.
	 */
	private void setOrgLogoOnly(TemplateMappedValues values, String orgShortCode, String accountShortCode,
			String subAccountShortCode) {
		String logoUrl = findLogoUrl(orgShortCode, accountShortCode, subAccountShortCode);
		if (logoUrl != null) {
			values.setLogoUrl(logoUrl);
			log.debug("Logo URL found: {} for payment ID: {}", logoUrl, values.getPaymentIdentifier());
		}
		else {
			log.debug("No logo URL found for org: {}, account: {}, subAccount: {}", orgShortCode, accountShortCode,
					subAccountShortCode);
		}
	}

	// Get Optum and Mastercard logo from cache (loaded at startup)
	private void setCompanyLogosFromCache(TemplateMappedValues values) {
		String optumLogo = this.logoCache.get(PaymentDistributionConstants.OPTUM_LOGO_RESOURCE);
		if (optumLogo != null) {
			values.setOptumLogo(optumLogo);
			log.debug("Optum logo set from cache for payment ID: {}", values.getPaymentIdentifier());
		}
		else {
			log.warn("Optum logo not found in cache for payment ID: {}", values.getPaymentIdentifier());
		}

		String mastercardLogo = this.logoCache.get(PaymentDistributionConstants.MASTERCARD_LOGO_RESOURCE);
		if (mastercardLogo != null) {
			values.setCardTypeLogo(mastercardLogo);
			log.debug("Mastercard logo set from cache for payment ID: {}", values.getPaymentIdentifier());
		}
		else {
			log.warn("Mastercard logo not found in cache for payment ID: {}", values.getPaymentIdentifier());
		}
	}

	// loaded company logo from resources
	private void loadAndSetLogo(String resourcePath, Consumer<String> setter, String logoName) {
		try {
			// Load from cache or load from resources
			String logo = this.logoCache.computeIfAbsent(resourcePath, (path) -> {
				String loadedLogo = loadLogoFromResources(path);
				return (loadedLogo != null) ? loadedLogo : "";
			});
			setter.accept(logo);
			log.debug("{} loaded", logoName);
		}
		catch (Exception ex) {
			log.warn("Failed to load {}: {}. Continuing with empty logo.", logoName,
					(ex.getMessage() != null) ? ex.getMessage() : ex.getClass().getSimpleName());
			setter.accept("");
		}
	}

	private void initializeCardDetails(TemplateMappedValues values) {
		values.setCardLastFourDigits(PaymentDistributionConstants.CARD_PAN);
		values.setCvv("");
		values.setExpiry("");
	}

	private String findLogoUrl(String orgShortCode, String accountShortCode, String subAccountShortCode) {
		try {
			String logoUrl = this.logoDetailsRepository
				.findLogoUrlByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(orgShortCode, accountShortCode,
						subAccountShortCode);
			if (logoUrl == null) {
				logoUrl = this.logoDetailsRepository
					.findLogoUrlByAccountShortCodeAndSubAccountShortCodeIsNullAndOrgShortCode(orgShortCode,
							accountShortCode);
			}
			if (logoUrl == null) {
				logoUrl = this.logoDetailsRepository
					.findLogoUrlByAccountShortCodeIsNullAndSubAccountShortCodeIsNullAndOrgShortCode(orgShortCode);
			}
			return logoUrl;
		}
		catch (Exception ex) {
			log.warn("Error finding logo URL for org: {}, account: {}, subAccount: {}. Error: {}", orgShortCode,
					accountShortCode, subAccountShortCode, ex.getMessage(), ex);
			return null;
		}
	}

	private String loadLogoFromResources(String resourcePath) {
		try {
			log.debug("Loading logo from resources: {}", resourcePath);

			ClassLoader classLoader = getClass().getClassLoader();
			try (InputStream inputStream = classLoader.getResourceAsStream(resourcePath)) {
				if (inputStream == null) {
					log.warn("Logo resource not found: {}", resourcePath);
					return null;
				}

				byte[] imageBytes = inputStream.readAllBytes();
				String imageType = getImageTypeFromPath(resourcePath);
				String base64Image = Base64.getEncoder().encodeToString(imageBytes);
				String dataUri = "data:image/" + imageType + ";base64," + base64Image;

				log.debug("Logo loaded from resources: {}", resourcePath);
				return dataUri;
			}
		}
		catch (Exception ex) {
			String errorMsg = (ex.getMessage() != null) ? ex.getMessage() : ex.getClass().getSimpleName();
			log.error("Error loading logo from resources {} [{}]: {}", resourcePath, ex.getClass().getSimpleName(),
					errorMsg, ex);
			return null;
		}
	}

	private String getImageTypeFromPath(String resourcePath) {
		if (resourcePath.endsWith(PaymentDistributionConstants.IMAGE_EXT_JPG)
				|| resourcePath.endsWith(PaymentDistributionConstants.IMAGE_EXT_JPEG)) {
			return PaymentDistributionConstants.IMAGE_TYPE_JPEG;
		}
		else if (resourcePath.endsWith(PaymentDistributionConstants.IMAGE_EXT_GIF)) {
			return PaymentDistributionConstants.IMAGE_TYPE_GIF;
		}
		return PaymentDistributionConstants.IMAGE_TYPE_PNG;
	}

	private DocumentCustomRemarks getCustomRemarks() {
		try {
			List<com.uhg.optum.financials.payments.payer.model.DocumentCustomRemarks> remarks = this.documentCustomRemarksRepository
				.findByCustomRemarks1NotNull();
			if (remarks != null && !remarks.isEmpty()) {
				return remarks.get(0);
			}
		}
		catch (Exception ex) {
			log.warn("Error fetching custom remarks from database: {}", ex.getMessage());
		}
		return null;
	}

	private String generateZipFileName(String orgShortCode, String correlationId, LocalDateTime batchCreatedAt) {
		// Format: orgShortCode_SummaryPage_CorrelationId_Timestamp.zip
		String timestamp = batchCreatedAt
			.format(DateTimeFormatter.ofPattern(PaymentDistributionConstants.DATE_FORMAT_TIMESTAMP));
		return String.format("%s_SummaryPage_%s_%s.zip", orgShortCode, correlationId, timestamp);
	}

	private String buildSupplementalRecord(PaymentRequest paymentRequest, DistributionRecord record, String paymentId,
			String paymentLogoUrl) {
		try {
			StringBuilder sb = new StringBuilder();
			String distributionMethod = (record.getDistributionMethod() != null) ? record.getDistributionMethod()
					: PaymentDistributionConstants.DEFAULT_DISTRIBUTION_METHOD;

			// Build required fields
			appendSupplementalField(sb, paymentId);
			appendSupplementalField(sb, getPaymentDate(paymentRequest, paymentId));
			appendSupplementalField(sb, getPaymentAmount(paymentRequest, paymentId));
			appendSupplementalField(sb, record.getCardGuid());
			appendSupplementalField(sb, generateTrackingNumber());
			appendSupplementalField(sb, record.getHtmlFileName());

			// Add logo - compute filename from URL hash (same as collectLogosForResult)
			String logoFilename = "";
			if (paymentLogoUrl != null && !paymentLogoUrl.isEmpty()) {
				String logoUrlHash = String.format("%08x", paymentLogoUrl.hashCode());
				logoFilename = logoUrlHash + ".png";
				log.debug("Logo filename computed for supplemental record: {} (from URL hash) for payment ID: {}",
						logoFilename, paymentId);
			}
			appendSupplementalField(sb, logoFilename);

			sb.append(distributionMethod).append("|");

			appendPayeeAndAddressFields(sb, paymentRequest, distributionMethod, paymentId);

			appendReturnAddressFields(sb, distributionMethod, paymentId);

			sb.append(distributionMethod).append("|");

			appendFaxFields(sb, distributionMethod, paymentId);

			log.debug("Supplemental record built for payment ID: {} with distributionMethod: {}", paymentId,
					distributionMethod);
			return sb.toString();
		}
		catch (Exception ex) {
			log.error("Error building supplemental record for payment ID: {}. Error: {}", paymentId, ex.getMessage());
			return null;
		}
	}

	private void appendSupplementalField(StringBuilder sb, String value) {
		String fieldValue = (value != null) ? value : "";
		sb.append(fieldValue).append("|");
	}

	private String getPaymentDate(PaymentRequest paymentRequest, String paymentId) {
		if (paymentRequest.getPaymentDetails() != null && paymentRequest.getPaymentDetails().getPaymentDate() != null) {
			return paymentRequest.getPaymentDetails().getPaymentDate().toString();
		}
		logMissingField("paymentDate", paymentId, PaymentDistributionConstants.LOG_FIELD_REQUIRED, null);
		return "";
	}

	private String getPaymentAmount(PaymentRequest paymentRequest, String paymentId) {
		if (paymentRequest.getPaymentDetails() != null && paymentRequest.getPaymentDetails().getAmount() != null) {
			return paymentRequest.getPaymentDetails().getAmount().toString();
		}
		logMissingField("paymentAmount", paymentId, PaymentDistributionConstants.LOG_FIELD_REQUIRED, null);
		return "";
	}

	private void appendPayeeAndAddressFields(StringBuilder sb, PaymentRequest paymentRequest, String distributionMethod,
			String paymentId) {
		String payeeName = (paymentRequest.getPayeeDetails() != null
				&& paymentRequest.getPayeeDetails().getName() != null) ? paymentRequest.getPayeeDetails().getName()
						: "";
		if (PaymentDistributionConstants.DISTRIBUTION_METHOD_PRINT.equalsIgnoreCase(distributionMethod)
				&& payeeName.isEmpty()) {
			logMissingField("mailTo", paymentId, PaymentDistributionConstants.LOG_FIELD_REQUIRED,
					PaymentDistributionConstants.LOG_DISTRIBUTION_PRINT);
		}
		sb.append(payeeName).append("|");

		com.uhg.optum.financials.payments.payer.model.Address addr = (paymentRequest.getPayeeDetails() != null)
				? paymentRequest.getPayeeDetails().getAddress() : null;

		appendAddressField(sb, (addr != null) ? addr.getLine1() : null,
				PaymentDistributionConstants.FIELD_MAIL_ADDRESS_LINE1, distributionMethod, paymentId);
		appendAddressField(sb, (addr != null) ? addr.getLine2() : null,
				PaymentDistributionConstants.FIELD_MAIL_ADDRESS_LINE2, distributionMethod, paymentId);
		appendAddressField(sb, (addr != null) ? addr.getCity() : null, PaymentDistributionConstants.FIELD_MAIL_CITY,
				distributionMethod, paymentId);
		appendAddressField(sb, (addr != null) ? addr.getState() : null, PaymentDistributionConstants.FIELD_MAIL_STATE,
				distributionMethod, paymentId);
		appendAddressField(sb, (addr != null) ? addr.getPostalCode() : null,
				PaymentDistributionConstants.FIELD_MAIL_POSTAL_CODE, distributionMethod, paymentId);
		appendAddressField(sb, (addr != null) ? addr.getCountry() : null,
				PaymentDistributionConstants.FIELD_MAIL_COUNTRY, distributionMethod, paymentId);
	}

	private void appendAddressField(StringBuilder sb, Object value, String fieldName, String distributionMethod,
			String paymentId) {
		String strValue = (value != null) ? value.toString() : "";
		if (PaymentDistributionConstants.DISTRIBUTION_METHOD_PRINT.equalsIgnoreCase(distributionMethod)
				&& strValue.isEmpty()) {
			logMissingField(fieldName, paymentId, PaymentDistributionConstants.LOG_FIELD_REQUIRED,
					PaymentDistributionConstants.LOG_DISTRIBUTION_PRINT);
		}
		sb.append(strValue).append("|");
	}

	private void appendReturnAddressFields(StringBuilder sb, String distributionMethod, String paymentId) {
		// Return fields are mostly empty for now
		String[] returnFields = { PaymentDistributionConstants.FIELD_RETURN_NAME,
				PaymentDistributionConstants.FIELD_RETURN_ADDRESS_LINE1,
				PaymentDistributionConstants.FIELD_RETURN_ADDRESS_LINE2, PaymentDistributionConstants.FIELD_RETURN_CITY,
				PaymentDistributionConstants.FIELD_RETURN_STATE,
				PaymentDistributionConstants.FIELD_RETURN_POSTAL_CODE };
		for (String field : returnFields) {
			if (PaymentDistributionConstants.DISTRIBUTION_METHOD_PRINT.equalsIgnoreCase(distributionMethod)) {
				logMissingField(field, paymentId, PaymentDistributionConstants.LOG_FIELD_REQUIRED,
						PaymentDistributionConstants.LOG_DISTRIBUTION_PRINT);
			}
			sb.append("|");
		}
	}

	private void appendFaxFields(StringBuilder sb, String distributionMethod, String paymentId) {
		if (PaymentDistributionConstants.DISTRIBUTION_METHOD_FAX.equalsIgnoreCase(distributionMethod)) {
			logMissingField("faxPhoneNumber", paymentId, PaymentDistributionConstants.LOG_FIELD_REQUIRED,
					PaymentDistributionConstants.LOG_DISTRIBUTION_FAX);
		}
		sb.append("|"); // faxPhoneNumber
		// faxHeaderName (optional)
	}

	private String generateTrackingNumber() {
		// To be replaced with actual tracking number generation logic. For now, using a
		// random UUID-based string.
		String uuid = UUID.randomUUID().toString().replace("-", "").toUpperCase();
		return String.format("01%s%d", uuid.substring(0, Math.min(22, uuid.length())),
				System.currentTimeMillis() % 10000);
	}

	private void addSupplementalFileToZip(String correlationId, LocalDateTime batchCreatedAt,
			List<String> supplementalRecords, ZipOutputStream zos) throws IOException {
		try {
			if (supplementalRecords == null || supplementalRecords.isEmpty()) {
				log.warn("No supplemental records to add for correlation ID: {}", correlationId);
				return;
			}

			// Generate supplemental file name:
			// supplemental_<correlationId>_<timestamp>.txt
			String timestamp = batchCreatedAt
				.format(DateTimeFormatter.ofPattern(PaymentDistributionConstants.DATE_FORMAT_TIMESTAMP));
			String supplementalFileName = String.format("supplemental_%s_%s.txt", correlationId, timestamp);

			// Build supplemental file content
			StringBuilder content = new StringBuilder();
			content.append(PaymentDistributionConstants.SUPPLEMENTAL_HEADER).append("\n");

			for (String record : supplementalRecords) {
				content.append(record).append("\n");
			}

			ZipEntry zipEntry = new ZipEntry(supplementalFileName);
			zos.putNextEntry(zipEntry);
			zos.write(content.toString().getBytes(StandardCharsets.UTF_8));
			zos.closeEntry();

			log.info("Supplemental file {} added to ZIP archive directly with {} payment records", supplementalFileName,
					supplementalRecords.size());
		}
		catch (IOException ex) {
			log.error("Error adding supplemental file to ZIP for correlation ID: {}. Error: {}", correlationId,
					ex.getMessage());
			throw ex;
		}
	}

	/**
	 * logs a missing field warning using a unified template.
	 * @param fieldName the name of the missing field
	 * @param paymentId the payment ID
	 * @param fieldLevel the level (required or recommended)
	 * @param distributionType the distribution type (null for generic, PRINT/FAX for
	 * specific)
	 */
	private void logMissingField(String fieldName, String paymentId, String fieldLevel, String distributionType) {
		if (distributionType != null) {
			// Use distribution-specific template
			log.warn(PaymentDistributionConstants.LOG_MISSING_FIELD_TEMPLATE, fieldLevel, fieldName, distributionType,
					paymentId);
		}
		else {
			// Use generic template without distribution type
			log.warn(PaymentDistributionConstants.LOG_MISSING_FIELD_GENERIC, fieldLevel, fieldName, paymentId);
		}
	}

	/**
	 * adds HTML content directly to ZIP without reading from disk.
	 * @param htmlContent the HTML content as string
	 * @param entryName the ZIP entry name
	 * @param zos the ZipOutputStream
	 */
	private void addHtmlContentToZip(String htmlContent, String entryName, ZipOutputStream zos) throws IOException {
		try {
			ZipEntry zipEntry = new ZipEntry(entryName);
			zos.putNextEntry(zipEntry);

			byte[] fileContent = htmlContent.getBytes(StandardCharsets.UTF_8);
			zos.write(fileContent);
			zos.closeEntry();

			log.debug("HTML file {} added to ZIP archive", entryName);
		}
		catch (IOException ex) {
			log.error("Error adding HTML file {} to ZIP: {}", entryName, ex.getMessage(), ex);
			throw ex;
		}
	}

	private void writeZipToBlobStorage(String zipFileName, Long batchId, List<PaymentRequest> paymentRequests,
			String correlationId, LocalDateTime batchCreatedAt, String optumLogo, String mastercardLogo, int batchSize)
			throws IOException {
		try {
			log.info(
					"Streaming ZIP file directly to blob storage for correlation ID: {} (processing {} payments in batches of {})",
					correlationId, paymentRequests.size(), batchSize);

			String blobPath = PaymentDistributionConstants.BLOB_STORAGE_DISTRIBUTION_FOLDER + "/" + zipFileName;
			BlobClient blobClient = this.blobServiceClient.getBlobContainerClient(this.blobContainerName)
				.getBlobClient(blobPath);
			List<String> supplementalRecords = new ArrayList<>();
			Set<String> writtenLogoEntries = new HashSet<>();
			int totalBatches = (paymentRequests.size() + batchSize - 1) / batchSize;
			MessageDigest messageDigest = createSha256Digest();

			try (OutputStream blobOutputStream = blobClient.getBlockBlobClient().getBlobOutputStream(true)) {
				DigestOutputStream digestOutputStream = new DigestOutputStream(blobOutputStream, messageDigest);
				ZipOutputStream zos = new ZipOutputStream(digestOutputStream);
				long startTime = System.currentTimeMillis();
				try {
					int filesWritten = writeConstantLogosToZip(optumLogo, mastercardLogo, writtenLogoEntries, zos);

					for (int startIndex = 0, batchNumber = 1; startIndex < paymentRequests
						.size(); startIndex += batchSize, batchNumber++) {
						int endIndex = Math.min(startIndex + batchSize, paymentRequests.size());
						List<PaymentRequest> batchPayments = paymentRequests.subList(startIndex, endIndex);

						long asyncStartTime = System.currentTimeMillis();
						List<HtmlGenerationResult> batchResults = generateHtmlAsyncParallel(batchId, batchPayments,
								optumLogo, mastercardLogo);
						long asyncDuration = System.currentTimeMillis() - asyncStartTime;

						long zipStartTime = System.currentTimeMillis();
						filesWritten += writeBatchResultsToZip(zos, batchResults, supplementalRecords,
								writtenLogoEntries, correlationId);
						long zipDuration = System.currentTimeMillis() - zipStartTime;

						log.info(
								"Processed HTML batch {}/{} for correlation ID: {} (payments {}-{}, async={}ms, zip={}ms)",
								batchNumber, totalBatches, correlationId, startIndex + 1, endIndex, asyncDuration,
								zipDuration);
					}

					addSupplementalFileToZip(correlationId, batchCreatedAt, supplementalRecords, zos);
					filesWritten++;

					long duration = System.currentTimeMillis() - startTime;
					log.info("ZIP file created and streamed to blob storage successfully: {} with {} files ({}ms)",
							blobPath, filesWritten, duration);
				}
				finally {
					// Finalize ZIP content without closing the underlying Azure blob
					// stream twice.
					zos.finish();
					zos.flush();
				}
			}

			String checksum = toHex(messageDigest.digest());
			writeChecksumToDistributionFolder(zipFileName, checksum);
			log.info("Checksum file created for ZIP {} using SHA-256: {}", zipFileName, checksum);

		}
		catch (IOException ex) {
			log.error("Error streaming ZIP to blob storage: {}", ex.getMessage(), ex);
			throw ex;
		}
	}

	private MessageDigest createSha256Digest() {
		try {
			return MessageDigest.getInstance("SHA-256");
		}
		catch (NoSuchAlgorithmException ex) {
			throw new IllegalStateException("SHA-256 algorithm is not available", ex);
		}
	}

	private String toHex(byte[] digestBytes) {
		StringBuilder sb = new StringBuilder(digestBytes.length * 2);
		for (byte digestByte : digestBytes) {
			sb.append(String.format("%02x", digestByte));
		}
		return sb.toString();
	}

	private void writeChecksumToDistributionFolder(String zipFileName, String checksum) throws IOException {
		String checksumFileName = zipFileName + ".sha256";
		String blobPath = PaymentDistributionConstants.BLOB_STORAGE_DISTRIBUTION_FOLDER + "/" + checksumFileName;
		String checksumContent = String.format("SHA256(%s)=%s", zipFileName, checksum);

		try {
			BlobClient blobClient = this.blobServiceClient.getBlobContainerClient(this.blobContainerName)
				.getBlobClient(blobPath);
			byte[] contentBytes = checksumContent.getBytes(StandardCharsets.UTF_8);

			try (InputStream inputStream = new java.io.ByteArrayInputStream(contentBytes)) {
				blobClient.upload(inputStream, contentBytes.length, true);
			}
		}
		catch (Exception ex) {
			log.error("Error writing checksum file for ZIP {}: {}", zipFileName, ex.getMessage(), ex);
			throw new IOException("Failed to write checksum file for ZIP: " + zipFileName, ex);
		}
	}

	private int writeConstantLogosToZip(String optumLogo, String mastercardLogo, Set<String> writtenLogoEntries,
			ZipOutputStream zos) throws IOException {
		int filesWritten = 0;
		filesWritten += writeLogoToZipIfNeeded(optumLogo,
				PaymentDistributionConstants.LOGOS_ZIP_FOLDER + "optum_logo.png", writtenLogoEntries, zos);
		filesWritten += writeLogoToZipIfNeeded(mastercardLogo,
				PaymentDistributionConstants.LOGOS_ZIP_FOLDER + "mastercard_logo.png", writtenLogoEntries, zos);
		return filesWritten;
	}

	private int writeBatchResultsToZip(ZipOutputStream zos, List<HtmlGenerationResult> batchResults,
			List<String> supplementalRecords, Set<String> writtenLogoEntries, String correlationId) throws IOException {
		int filesWritten = 0;

		for (HtmlGenerationResult result : batchResults) {
			if (!result.isSuccess()) {
				log.error("HTML generation failed for payment ID: {}. Error: {}", result.getPaymentId(),
						result.getErrorMessage());
				continue;
			}

			filesWritten += writeOrgLogoToZipIfNeeded(result, writtenLogoEntries, correlationId, zos);

			if (result.getHtmlContent() != null) {
				stageHtmlToBlobIfEnabled(result.getHtmlContent(), result.getHtmlFileName(), correlationId);
				addHtmlContentToZip(result.getHtmlContent(),
						PaymentDistributionConstants.HTML_ZIP_FOLDER + result.getHtmlFileName(), zos);
				filesWritten++;
			}

			if (result.getSupplementalRecord() != null && !result.getSupplementalRecord().isEmpty()) {
				supplementalRecords.add(result.getSupplementalRecord());
			}

			result.setHtmlContent(null);
			result.getLogoData().clear();
			result.setOrgLogoSource(null);
		}

		return filesWritten;
	}

	private void stageHtmlToBlobIfEnabled(String htmlContent, String htmlFileName, String correlationId)
			throws IOException {
		if (!this.htmlGenerationStagingEnabled) {
			return;
		}

		writeHtmlToStagingArea(htmlContent, htmlFileName, correlationId);
	}

	private void stageOrgLogoToBlobIfEnabled(byte[] logoBytes, String logoHash, String correlationId)
			throws IOException {
		if (!this.htmlGenerationStagingEnabled || logoBytes == null || logoBytes.length == 0
				|| !isValidLogoData(logoHash)) {
			return;
		}

		writeLogoToStagingArea(logoBytes, logoHash, correlationId);
	}

	private void writeHtmlToStagingArea(String htmlContent, String htmlFileName, String correlationId)
			throws IOException {
		try {
			String blobPath = buildTmpBlobPath(correlationId, "html", htmlFileName);
			BlobClient blobClient = this.blobServiceClient.getBlobContainerClient(this.blobContainerName)
				.getBlobClient(blobPath);

			byte[] contentBytes = htmlContent.getBytes(StandardCharsets.UTF_8);
			try (InputStream inputStream = new java.io.ByteArrayInputStream(contentBytes)) {
				blobClient.upload(inputStream, contentBytes.length, true);
			}

			log.debug("HTML file written to blob staging: {}", blobPath);
		}
		catch (Exception ex) {
			log.error("Error writing HTML to blob staging for file {}: {}", htmlFileName, ex.getMessage(), ex);
			throw new IOException("Failed to write HTML to blob staging: " + ex.getMessage(), ex);
		}
	}

	private void writeLogoToStagingArea(byte[] logoBytes, String logoHash, String correlationId) throws IOException {
		try {
			String blobPath = buildTmpBlobPath(correlationId, "logos", logoHash + ".png");
			BlobClient blobClient = this.blobServiceClient.getBlobContainerClient(this.blobContainerName)
				.getBlobClient(blobPath);

			try (InputStream inputStream = new java.io.ByteArrayInputStream(logoBytes)) {
				blobClient.upload(inputStream, logoBytes.length, true);
			}

			log.debug("Organization logo written to blob staging: {}", blobPath);
		}
		catch (Exception ex) {
			log.error("Error writing organization logo to blob staging for hash {}: {}", logoHash, ex.getMessage(), ex);
			throw new IOException("Failed to write organization logo to blob staging: " + ex.getMessage(), ex);
		}
	}

	private String buildTmpBlobPath(String correlationId, String subFolder, String fileName) {
		return PaymentDistributionConstants.BLOB_STORAGE_TMP_FOLDER + "/" + correlationId + "/" + subFolder + "/"
				+ fileName;
	}

	private int writeOrgLogoToZipIfNeeded(HtmlGenerationResult result, Set<String> writtenLogoEntries,
			String correlationId, ZipOutputStream zos) throws IOException {
		if (!isValidLogoData(result.getOrgLogoSource()) || !isValidLogoData(result.getOrgLogoHash())) {
			return 0;
		}

		String zipEntryName = PaymentDistributionConstants.LOGOS_ZIP_FOLDER + result.getOrgLogoHash() + ".png";
		try {
			return writeOrgLogoToZipAndStageIfNeeded(result.getOrgLogoSource(), result.getOrgLogoHash(), zipEntryName,
					writtenLogoEntries, correlationId, zos);
		}
		catch (IOException ex) {
			log.warn("Failed to write organization logo {} to ZIP. Error: {}", zipEntryName, ex.getMessage());
			return 0;
		}
	}

	private int writeOrgLogoToZipAndStageIfNeeded(String logoSource, String logoHash, String zipEntryName,
			Set<String> writtenLogoEntries, String correlationId, ZipOutputStream zos) throws IOException {
		if (!isValidLogoData(logoSource) || writtenLogoEntries.contains(zipEntryName)) {
			return 0;
		}

		byte[] logoBytes = resolveLogoBytes(logoSource);
		if (logoBytes == null || logoBytes.length == 0) {
			return 0;
		}

		stageOrgLogoToBlobIfEnabled(logoBytes, logoHash, correlationId);

		ZipEntry zipEntry = new ZipEntry(zipEntryName);
		zos.putNextEntry(zipEntry);
		zos.write(logoBytes);
		zos.closeEntry();
		writtenLogoEntries.add(zipEntryName);
		log.debug("Organization logo written to ZIP: {} (size: {} bytes)", zipEntryName, logoBytes.length);
		return 1;
	}

	private int writeLogoToZipIfNeeded(String logoSource, String zipEntryName, Set<String> writtenLogoEntries,
			ZipOutputStream zos) throws IOException {
		if (!isValidLogoData(logoSource) || writtenLogoEntries.contains(zipEntryName)) {
			return 0;
		}

		byte[] logoBytes = resolveLogoBytes(logoSource);
		if (logoBytes == null || logoBytes.length == 0) {
			return 0;
		}

		ZipEntry zipEntry = new ZipEntry(zipEntryName);
		zos.putNextEntry(zipEntry);
		zos.write(logoBytes);
		zos.closeEntry();
		writtenLogoEntries.add(zipEntryName);
		log.debug("Logo written to ZIP: {} (size: {} bytes)", zipEntryName, logoBytes.length);
		return 1;
	}

	private byte[] resolveLogoBytes(String logoSource) throws IOException {
		if (!isValidLogoData(logoSource)) {
			return null;
		}

		if (logoSource.startsWith("data:image")) {
			return extractLogoBytesFromDataUri(logoSource);
		}

		return downloadLogoBytes(logoSource);
	}

	@PreDestroy
	public void shutdown() {
		if (this.htmlGenerationExecutor != null) {
			log.info("Shutting down HTML generation thread pool");
			this.htmlGenerationExecutor.shutdown();
			try {
				if (!this.htmlGenerationExecutor.awaitTermination(30, TimeUnit.SECONDS)) {
					log.warn("Thread pool did not terminate within 30 seconds, forcing shutdown");
					this.htmlGenerationExecutor.shutdownNow();
				}
				log.info("HTML generation thread pool shut down successfully");
			}
			catch (InterruptedException ex) {
				log.error("Interrupted while waiting for thread pool shutdown", ex);
				this.htmlGenerationExecutor.shutdownNow();
				Thread.currentThread().interrupt();
			}
		}
	}

}
