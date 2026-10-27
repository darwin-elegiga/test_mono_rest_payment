package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.identity.WorkloadIdentityCredentialBuilder;
import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.BlobServiceClient;
import com.azure.storage.blob.sas.BlobSasPermission;
import com.azure.storage.blob.sas.BlobServiceSasSignatureValues;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.google.gson.Gson;
import com.uhg.optum.financials.payments.organization.events.SubAccountRegistered;
import com.uhg.optum.financials.payments.organization.models.Address;
import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.payments.payer.constant.EventTypeMapping;
import com.uhg.optum.financials.payments.payer.dto.AcquirerAddress;
import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.document.DocumentRequest;
import com.uhg.optum.financials.payments.payer.dto.document.DocumentRequestCDE;
import com.uhg.optum.financials.payments.payer.dto.document.DocumentResponse;
import com.uhg.optum.financials.payments.payer.dto.document.MetaData;
import com.uhg.optum.financials.payments.payer.dto.document.SummaryDocumentInputDto;
import com.uhg.optum.financials.payments.payer.dto.document.TemplateMappedValues;
import com.uhg.optum.financials.payments.payer.dto.subaccount.SubAccountDto;
import com.uhg.optum.financials.payments.payer.enums.LogoType;
import com.uhg.optum.financials.payments.payer.exception.DocumentGenException;
import com.uhg.optum.financials.payments.payer.model.CompanyLogoDetails;
import com.uhg.optum.financials.payments.payer.model.DocumentCustomRemarks;
import com.uhg.optum.financials.payments.payer.model.DocumentResponseDetails;
import com.uhg.optum.financials.payments.payer.model.PayeeDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.repository.CompanyLogoDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.DocumentCustomRemarksRepository;
import com.uhg.optum.financials.payments.payer.repository.DocumentResponseDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.LogoDetailsRepository;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.BarCodeService;
import com.uhg.optum.financials.payments.payer.service.SummaryPageService;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import com.uhg.optum.financials.shared.core.config.KeyVaultConfig;
import com.uhg.optum.financials.shared.core.http.BearerTokenInterceptor;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import com.uhg.optum.financials.shared.client.constant.RetentionType;
import com.uhg.optum.financials.shared.client.events.EventBusEvent;
import com.uhg.optum.financials.shared.core.storage.AzureStorageUtil;
import com.uhg.optum.financials.shared.eventbus.kafka.service.EventBusProducer;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.StringUtils;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.core.io.FileSystemResource;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.util.ResourceUtils;

import javax.validation.constraints.NotNull;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.math.BigDecimal;
import java.time.Duration;
import java.time.Instant;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.time.format.DateTimeFormatter;
import java.util.Collections;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import java.util.stream.Collectors;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.VIRTUAL_CARD;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR_MESSAGE;
import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.Constants.NEW_LINE_ONLY;
import static com.uhg.optum.financials.payments.payer.constant.Constants.VIRTUAL_CARD_NON_CDE_SERVICE;
import static com.uhg.optum.financials.payments.payer.constant.DocumentConstants.SLASH_DELIMITER;
import static com.uhg.optum.financials.payments.payer.constant.DocumentConstants.SUMMARY;
import static com.uhg.optum.financials.payments.payer.constant.DocumentConstants.TENANT_HEADER;
import static com.uhg.optum.financials.payments.payer.constant.DocumentConstants.SUMMARY_PAGE_DOC_TYPE;
import static com.uhg.optum.financials.payments.payer.constant.ErrorCodes.P3;

@Service
@Slf4j
public class SummaryPageServiceImpl implements SummaryPageService {

	@Value("${document.template.file-name}")
	private String templateFileName;

	@Value("${endpoint.documentSystem.documentGeneration}")
	private String documentGenerationServiceUrl;

	private final ResilientRestClient documentGenerationClient;

	private final SubAccountRepository subAccountRepository;

	private final DocumentResponseDetailsRepository documentResponseDetailsRepository;

	private final LogoDetailsRepository logoDetailsRepository;

	private final CompanyLogoDetailsRepository companyLogoDetailsRepository;

	private final DocumentCustomRemarksRepository documentCustomRemarksRepository;

	private final PaymentRequestRepository paymentRequestRepository;

	private final ZetaApiService zetaApiService;

	private static final Gson JSON = new Gson();

	private final BlobServiceClient blobServiceClient;

	private final BarCodeService barCodeService;

	@Value("${storage.containerName}")
	private String blobContainerName;

	@Value("${document.template.image-folder}")
	private String imagePath;

	private final EventBusProducer eventBusProducer;

	private final LogUtility logUtility;

	private final VirtualCardUtil virtualCardUtil;

	public SummaryPageServiceImpl(ResilientRestClientFactory resilientRestClientFactory,
			SubAccountRepository subAccountRepository,
			DocumentResponseDetailsRepository documentResponseDetailsRepository,
			LogoDetailsRepository logoDetailsRepository,
			DocumentCustomRemarksRepository documentCustomRemarksRepository,
			PaymentRequestRepository paymentRequestRepository,
			CompanyLogoDetailsRepository companyLogoDetailsRepository, ZetaApiService zetaApiService,
			BlobServiceClient blobServiceClient, BarCodeService barCodeService,
			@Qualifier("adminProducer") EventBusProducer eventBusProducer, LogUtility logUtility,
			VirtualCardUtil virtualCardUtil, @Value("${azure.client.id:}") String azureClientId,
			@Value("${azure.tenant.id:}") String azureTenantId,
			@Value("${azure.federated.token.file:}") String azureTokenFile,
			@Value("${spring.cloud.azure.active-directory.credential.client-id:}") String azureAppClientId,
			@Value("${keyvault.kind:Local}") String keyVaultKind) {
		this.documentGenerationClient = resilientRestClientFactory.getClient("documentGeneration", (builder) -> {
			if (StringUtils.equalsIgnoreCase(keyVaultKind, KeyVaultConfig.VaultKind.Azure.name())) {
				builder.requestInterceptor(
						new BearerTokenInterceptor(new WorkloadIdentityCredentialBuilder().clientId(azureClientId)
							.tenantId(azureTenantId)
							.tokenFilePath(azureTokenFile)
							.build(), azureAppClientId));
			}
		});
		this.subAccountRepository = subAccountRepository;
		this.documentResponseDetailsRepository = documentResponseDetailsRepository;
		this.logoDetailsRepository = logoDetailsRepository;
		this.documentCustomRemarksRepository = documentCustomRemarksRepository;
		this.paymentRequestRepository = paymentRequestRepository;
		this.companyLogoDetailsRepository = companyLogoDetailsRepository;
		this.zetaApiService = zetaApiService;
		this.blobServiceClient = blobServiceClient;
		this.barCodeService = barCodeService;
		this.eventBusProducer = eventBusProducer;
		this.logUtility = logUtility;
		this.virtualCardUtil = virtualCardUtil;
	}

	@Override
	public void processSummaryDocumentMessage(PaymentRequest paymentRequestDto, LocalDate expiryDate, UUID cardGuid) {
		String networkPaymentId = null;
		String paymentId = null;
		try {
			PaymentDetails paymentDetails = paymentRequestDto.getPaymentDetails();
			paymentId = (paymentDetails != null) ? paymentDetails.getId() : null;
			com.uhg.optum.financials.payments.payer.model.Address address = null;

			var shippingDetails = paymentRequestDto.getShippingDetails();
			var payeeDetails = paymentRequestDto.getPayeeDetails();
			if (shippingDetails != null) {
				address = shippingDetails.getAddress();
			}
			if (address == null && payeeDetails != null) {
				address = payeeDetails.getAddress();
			}
			SummaryDocumentInputDto summaryDocumentInputDto = this.paymentRequestRepository
				.findDocDetailsByIssuerPaymentIdAndCorrelationId(paymentDetails.getIssuerPaymentId(),
						paymentRequestDto.getCorrelationId());
			networkPaymentId = summaryDocumentInputDto.getNetworkPaymentId();
			generateUsingMultiPartRequest(summaryDocumentInputDto,
					Collections.singletonList(paymentDetails.getIssuerPaymentId()), paymentDetails.getAmount(),
					summaryDocumentInputDto.getNetworkPaymentId(), true, expiryDate, paymentDetails.getPin(),
					paymentDetails.getId(), cardGuid, address);

		}
		catch (Exception ex) {
			log.error("Error occurred while processing summary document message for network payment id: {}. Error: {}",
					sanitizedResponse(networkPaymentId), ex.getMessage());
			handleSummaryProcessException(paymentId, ex);
		}

	}

	private void handleSummaryProcessException(String paymentId, Exception ex) {
		String resolvedPaymentId = (paymentId != null) ? paymentId : "UNKNOWN";
		this.logUtility.logMessageWithPriority(
				"Summary page generation failed for paymentId: " + resolvedPaymentId + ERROR_MESSAGE + ex.getMessage(),
				ERROR, log, P3);
		// Rethrow instead of swallowing: the caller (CreateCardWebhookHandler) relies on
		// this failing visibly so it doesn't advance completion_stage past
		// DOCUMENT_GENERATED when generation actually failed. PE is set by that caller's
		// own exception handling, so it isn't duplicated here.
		throw new DocumentGenException("Summary document generation failed for paymentId: " + resolvedPaymentId
				+ ERROR_MESSAGE + ex.getMessage());
	}

	private void generateUsingMultiPartRequest(SummaryDocumentInputDto cardSummaryRequest,
			List<String> issuerPaymentIdList, BigDecimal consolidatedAmount, String networkPaymentId, boolean isMasked,
			LocalDate expiryDate, String pin, String paymentId, UUID cardGuid,
			com.uhg.optum.financials.payments.payer.model.Address shippingAddress) throws Exception {
		MultiValueMap<String, Object> body = new LinkedMultiValueMap<>();

		try {
			CardDetailsDTO cardDetailsDTO = fetchVirtualCardDetails(cardGuid);
			DocumentRequest documentRequest = transformData(cardSummaryRequest, issuerPaymentIdList, consolidatedAmount,
					networkPaymentId, isMasked, expiryDate, pin, paymentId, cardGuid, shippingAddress, cardDetailsDTO);

			String issuerPaymentIdListString = issuerPaymentIdList.stream().collect(Collectors.joining(", "));
			// sending event to CDE for generating unmasked pdf
			sendEventToCDE(documentRequest, cardGuid, paymentId, issuerPaymentIdListString,
					cardSummaryRequest.getOrgShortCode(), cardDetailsDTO, networkPaymentId);
			ObjectMapper objectMapper = new ObjectMapper();
			String json = objectMapper.writeValueAsString(documentRequest);
			log.info("Document request for network payment id: {}", json);
			body.add("incomingData", documentRequest);
			body.add("outputFileName", SUMMARY + networkPaymentId);
			body.add("multipartFile", new FileSystemResource(ResourceUtils.getFile(this.templateFileName)));

			// Set the headers
			HttpHeaders headers = new HttpHeaders();
			headers.setContentType(MediaType.MULTIPART_FORM_DATA);
			headers.set(TENANT_HEADER, cardSummaryRequest.getOrgShortCode());
			ResponseEntity<String> response = this.documentGenerationClient.execute((client) -> client.post()
				.uri(this.documentGenerationServiceUrl)
				.headers((httpHeaders) -> httpHeaders.addAll(headers))
				.body(body)
				.retrieve()
				.toEntity(String.class));
			DocumentResponse documentResponse = objectMapper.readValue(response.getBody(), DocumentResponse.class);

			// Process the response
			if (response.getStatusCode().is2xxSuccessful()) {
				log.info("Document generated successfully for network payment id: {}",
						sanitizedResponse(networkPaymentId));
			}
			else {
				log.error("Document generation request failed for network payment id: {}. Status code: {}",
						sanitizedResponse(networkPaymentId), response.getStatusCode());
			}
			if (documentResponse != null) {
				saveDocumentResponseDetails(documentResponse, networkPaymentId);
			}
		}
		catch (

		FileNotFoundException ex) {
			throw new DocumentGenException("Template file not found: " + ex.getMessage());

		}
		catch (Exception ex) {
			throw new DocumentGenException("IO Exception occurred while generating document: " + ex.getMessage());
		}
	}

	private DocumentRequest transformData(SummaryDocumentInputDto summaryDocumentInputDto,
			List<String> issuerPaymentIdList, BigDecimal consolidatedAmount, String networkPaymentId, boolean isMasked,
			LocalDate expiryDate, String pin, String paymentId, UUID cardGuid,
			com.uhg.optum.financials.payments.payer.model.Address shippingAddress, CardDetailsDTO cardDetailsDTO)
			throws Exception {
		DocumentRequest documentRequest = new DocumentRequest();
		String key = SLASH_DELIMITER + summaryDocumentInputDto.getOrgShortCode() + SLASH_DELIMITER
				+ summaryDocumentInputDto.getAccountShortCode() + SLASH_DELIMITER
				+ summaryDocumentInputDto.getSubAccountShortCode();
		documentRequest.setKey(key);
		documentRequest.setIdentifier(UUID.randomUUID());
		documentRequest.setIsPCI(false);
		documentRequest.setDocType(SUMMARY_PAGE_DOC_TYPE);

		String last4Digits = getLast4Digits(cardDetailsDTO);
		TemplateMappedValues templateMappedValues = new TemplateMappedValues();
		templateMappedValues.setPaymentIdentifier(paymentId);
		templateMappedValues.setPaymentType("Zeta");
		templateMappedValues.setCardLastFourDigits(last4Digits);
		templateMappedValues.setPin(pin);
		templateMappedValues.setCvv("");
		templateMappedValues.setExpiry(addSlashInExpiryDate(cardDetailsDTO.getExpiryMMYY()));
		String issuerPaymentIdListString = issuerPaymentIdList.stream().collect(Collectors.joining(", "));
		templateMappedValues.setIssuerPaymentIdList(issuerPaymentIdListString.trim());
		templateMappedValues.setNetworkPaymentId(networkPaymentId);
		// Set SubPayer
		SubAccount subAccount = this.subAccountRepository
			.findByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(summaryDocumentInputDto.getAccountShortCode(),
					summaryDocumentInputDto.getSubAccountShortCode(), summaryDocumentInputDto.getOrgShortCode())
			.orElse(null);

		SubAccountDto subPayer = new SubAccountDto();
		if (subAccount != null) {

			SubAccountRegistered subAccountRegistered = new SubAccountRegistered();
			subAccountRegistered.setName(subAccount.getName());

			Address address = new Address();
			address.setAddressLine1(subAccount.getAddress().getLine1());
			address.setAddressLine2(subAccount.getAddress().getLine2());
			address.setCity(subAccount.getAddress().getCity());
			address.setState(subAccount.getAddress().getState());
			address.setPostalCode(subAccount.getAddress().getPostalCode());
			Address returnMailingAddress = new Address();
			returnMailingAddress.setAddressLine1(subAccount.getReturnMailingAddress().getLine1());
			returnMailingAddress.setAddressLine2(subAccount.getReturnMailingAddress().getLine2());
			returnMailingAddress.setCity(subAccount.getReturnMailingAddress().getCity());
			returnMailingAddress.setState(subAccount.getReturnMailingAddress().getState());
			returnMailingAddress.setPostalCode(subAccount.getReturnMailingAddress().getPostalCode());
			subAccountRegistered.setAddress(address);
			subAccountRegistered.setReturnMailingAddress(returnMailingAddress);
			subAccountRegistered.setPhoneNumber(subAccount.getPhoneNumber());
			subPayer.setSubAccountRegistered(subAccountRegistered);
			templateMappedValues.setSubAccountDto(subPayer);
		}
		// set Payee
		PayeeDetails payeeDetails = new PayeeDetails();
		payeeDetails.setName(summaryDocumentInputDto.getPayeeName());
		payeeDetails.setTaxId(String.format("*****%s", summaryDocumentInputDto.getPayeeTaxId().substring(5)));
		payeeDetails.setNpi(summaryDocumentInputDto.getPayeeNpi());
		payeeDetails.setReceiverId(summaryDocumentInputDto.getPayeeReceiverId());
		payeeDetails.setType(summaryDocumentInputDto.getPayeeType());

		com.uhg.optum.financials.payments.payer.model.Address payeeAddress = new com.uhg.optum.financials.payments.payer.model.Address();
		payeeAddress.setLine1(summaryDocumentInputDto.getAddressLine1());
		payeeAddress.setLine2(summaryDocumentInputDto.getAddressLine2());
		payeeAddress.setCity(summaryDocumentInputDto.getAddressCity());
		payeeAddress.setState(summaryDocumentInputDto.getAddressState());
		payeeAddress.setPostalCode(summaryDocumentInputDto.getAddressPostalCode());
		payeeAddress.setCountry(summaryDocumentInputDto.getAddressCountry());

		payeeDetails.setAddress(payeeAddress);

		templateMappedValues.setPayeeDetails(payeeDetails);

		templateMappedValues.setAmount(consolidatedAmount.toPlainString());

		DateTimeFormatter dateFormatter = DateTimeFormatter.ofPattern("MM/dd/yyyy");
		templateMappedValues.setDate(summaryDocumentInputDto.getPaymentDate().format(dateFormatter));

		templateMappedValues.setExpiryDate(expiryDate.format(dateFormatter));

		// set MetaData
		MetaData documentMetaData = new MetaData();
		documentMetaData.setFileSize("20kb");
		documentMetaData.setFileType("pdf");
		documentRequest.setMetadata(documentMetaData);

		populateCustomRemarks(templateMappedValues, summaryDocumentInputDto,
				(subAccount != null) ? subAccount.getName() : null,
				(subAccount != null) ? subAccount.getClaimOfficePhoneNumber() : null);

		AcquirerAddress acquirerAddress = getShippingAddress(shippingAddress);
		templateMappedValues.setBarCodeUrl(this.barCodeService.generateBarCode(acquirerAddress));
		documentRequest.setTemplateMappedValues(templateMappedValues);
		return documentRequest;
	}

	private String getLast4Digits(CardDetailsDTO cardDetailsDTO) throws Exception {
		String maskedPan = cardDetailsDTO.getMaskedPan();
		if (maskedPan != null && maskedPan.length() >= 4) {
			return maskedPan.substring(maskedPan.length() - 4);
		}
		return "****";
	}

	private CardDetailsDTO fetchVirtualCardDetails(UUID cardGuid) throws Exception {
		return this.zetaApiService.getCardDetails(cardGuid);
	}

	private void saveDocumentResponseDetails(DocumentResponse documentResponse, String networkPaymentId) {
		DocumentResponseDetails documentResponseDetails = new DocumentResponseDetails();
		documentResponseDetails.setNetworkPaymentId(networkPaymentId);
		documentResponseDetails.setDocumentUrl(documentResponse.getDocumentUrl());
		documentResponseDetails.setDocumentId(documentResponse.getDocumentId());
		documentResponseDetails.setMetaData(documentResponse.getMetadata().toString());
		documentResponseDetails.setDocType(documentResponse.getDocumentType());
		documentResponseDetails.setErrorDesc(documentResponse.getErrorDesc());
		documentResponseDetails.setCreatedBy(AppConstants.VIRTUAL_CARD);
		documentResponseDetails.setCreatedDt(LocalDateTime.now());
		documentResponseDetails.setLastChangedBy(AppConstants.VIRTUAL_CARD);
		documentResponseDetails.setLastChangedDt(LocalDateTime.now());
		this.documentResponseDetailsRepository.save(documentResponseDetails);
	}

	private void populateCustomRemarks(TemplateMappedValues templateMappedValues,
			SummaryDocumentInputDto summaryDocumentInputDto, String payerName, String payerPhone)
			throws DocumentGenException {

		// wildcarding the custom remarks until we have the actual data from organization
		// domain
		try {
			DocumentCustomRemarks documentCustomRemarks = getCustomRemarks(summaryDocumentInputDto.getOrgShortCode(),
					summaryDocumentInputDto.getAccountShortCode(), summaryDocumentInputDto.getSubAccountShortCode());
			if (documentCustomRemarks == null) {
				throw new DocumentGenException("Custom remarks not found for the given account");
			}
			String logUrl = findLogoUrl(summaryDocumentInputDto.getOrgShortCode(),
					summaryDocumentInputDto.getAccountShortCode(), summaryDocumentInputDto.getSubAccountShortCode());
			createSASURL();
			String mastercardLogoUrl = this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.MASTERCARD.name());
			String optumLogoUrl = this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.OPTUM.name());
			String stepsLogoUrl = this.companyLogoDetailsRepository.getLogoUrlByType(LogoType.INSTRUCTIONS.name());
			if (logUrl == null) {
				throw new DocumentGenException("Logo URL not found for the given account");
			}

			templateMappedValues.setLogoUrl(logUrl);
			String customRemarks1 = documentCustomRemarks.getCustomRemarks1();
			if (payerName != null && customRemarks1 != null) {
				customRemarks1 = customRemarks1.replaceAll("(?i)\\[payer name\\]", payerName);
			}
			templateMappedValues.setCustomRemarks1(formatCustomRemarksForHtml(customRemarks1));

			String remark2 = documentCustomRemarks.getCustomRemarks2();
			if (remark2 != null && payerName != null) {
				remark2 = remark2.replaceAll("(?i)\\[payer name\\]", payerName);
			}
			if (remark2 != null && payerPhone != null) {
				remark2 = remark2.replaceAll("(?i)\\[payer phone\\]", payerPhone);
			}
			remark2 = Optional.ofNullable(remark2).orElse("");

			List<String> remarkList = NEW_LINE_ONLY.splitAsStream(remark2)
				.map((s) -> s.replaceFirst("^\\s*\\d+\\.\\s*", ""))
				.map(String::trim)
				.filter((s) -> !s.isBlank())
				.map(this::formatCustomRemarksForHtml)
				.collect(java.util.stream.Collectors.toList());

			templateMappedValues.setCustomRemarks2(remarkList);
			templateMappedValues.setOptumLogo(optumLogoUrl);
			templateMappedValues.setCardTypeLogo(mastercardLogoUrl);
			templateMappedValues.setInstructions(stepsLogoUrl);

		}
		catch (Exception ex) {
			throw new DocumentGenException(
					"An unexpected error occurred while populating custom remarks: " + ex.getMessage());
		}
	}

	private String formatCustomRemarksForHtml(String customRemarks1) {
		if (customRemarks1 == null) {
			return null;
		}
		String normalized = customRemarks1.replace("\r\n", "\n").replace("\r", "\n").replace("\\n", "\n");
		// Match http://, https://, or www. prefixed URLs
		String withLinks = normalized.replaceAll("((?:https?://|www\\.)[\\w\\-._~:/?#\\[\\]@!$&'()*+,;=%]+)",
				"<a href=\"$1\" target=\"_blank\">$1</a>");
		return withLinks.replace("\n", "<br/>");
	}

	private String findLogoUrl(String orgShortCode, String accountShortCode, String subAccountShortCode) {
		String logoUrl = this.logoDetailsRepository.findLogoUrlByAccountShortCodeAndSubAccountShortCodeAndOrgShortCode(
				orgShortCode, accountShortCode, subAccountShortCode);
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

	public DocumentCustomRemarks getCustomRemarks() {
		return this.documentCustomRemarksRepository.findByCustomRemarks1NotNull().get(0);
	}

	public String sanitizedResponse(String response) {
		return (response != null) ? response.replaceAll("[^a-zA-Z0-9-_]", "") : "null";
	}

	private String maskCardNumber(String cardNumber) {
		if (cardNumber == null || cardNumber.length() != 16) {
			throw new IllegalArgumentException("Invalid card number: must be 16 digits and not null.");
		}
		return String.format("**** **** **** %s", cardNumber.substring(12));
	}

	private static String addSlashInExpiryDate(String expiryDate) {
		if (expiryDate == null || expiryDate.isEmpty()) {
			throw new IllegalArgumentException("expiryDate cannot be null or empty");
		}
		int mid = expiryDate.length() / 2;
		return expiryDate.substring(0, mid) + "/" + expiryDate.substring(mid);
	}

	private static String addHiphenInCardNumber(String pan) {
		if (pan == null || pan.isEmpty()) {
			throw new IllegalArgumentException("cardnumber cannot be null or empty");
		}
		return pan.replaceAll("(.{4})", "$1-").replaceAll("-$", "");
	}

	public void createSASURL() {
		log.info("Starting SAS URL creation process...");
		File folder = getResourceFolder(this.imagePath);

		if (folder == null || !folder.exists()) {
			log.error("Images folder not found or inaccessible.");
			return;
		}

		File[] files = folder.listFiles();
		if (files == null || files.length == 0) {
			log.info("No files found in the images folder.");
			return;
		}
		for (File file : files) {
			if (file.isFile()) {
				processFile(file);
			}
		}
		log.info("SAS URL creation process completed.");
	}

	private File getResourceFolder(String folderName) {
		// If the folderName is an absolute path, use it directly
		File folder = new File(folderName);
		if (folder.isAbsolute()) {
			if (folder.exists() && folder.isDirectory()) {
				return folder;
			}
			else {
				log.error("Folder '{}' not found or inaccessible at absolute path.", folderName);
				return null;
			}
		}
		// Otherwise, try to load from classpath resources
		try {
			ClassLoader classLoader = getClass().getClassLoader();
			java.net.URL resource = classLoader.getResource(folderName);
			if (resource == null) {
				log.error("Folder '{}' not found in resources directory.", folderName);
				return null;
			}
			File resourceFolder = new File(resource.getFile());
			if (resourceFolder.exists() && resourceFolder.isDirectory()) {
				return resourceFolder;
			}
			else {
				log.error("Folder '{}' found in resources but is not a directory.", folderName);
				return null;
			}
		}
		catch (Exception ex) {
			log.error("Error accessing folder '{}' in resources directory.", folderName, ex);
			return null;
		}
	}

	private void processFile(File file) {
		String blobName = file.getName();
		try (FileInputStream fileInputStream = new FileInputStream(file)) {
			BlobContainerClient containerClient = this.blobServiceClient.getBlobContainerClient(this.blobContainerName);

			// Upload the file to Blob Storage
			AzureStorageUtil.createBlobItem(containerClient, blobName, fileInputStream.readAllBytes());

			// Generate SAS URL
			String sasURL = generateSasUrl(containerClient, blobName);

			// Save details to the database
			saveCompanyLogoDetails(blobName, sasURL);

			log.info("Successfully processed file: {}", blobName);
		}
		catch (IOException ex) {
			log.error("Failed to upload file: {}", blobName, ex);
		}
	}

	private void saveCompanyLogoDetails(String blobName, String sasURL) {
		LogoType logoType;
		switch (blobName) {
			case "optumLogo.png":
				logoType = LogoType.OPTUM;
				break;
			case "masterCard.png":
				logoType = LogoType.MASTERCARD;
				break;
			case "instructions.png":
				logoType = LogoType.INSTRUCTIONS;
				break;
			default:
				log.info("Skipping file: {} as it does not match any known logo types.", blobName);
				return;
		}
		saveOrUpdateCompanyLogoDetails(logoType, sasURL);
	}

	private void saveOrUpdateCompanyLogoDetails(LogoType logoType, String sasURL) {
		CompanyLogoDetails companyLogoDetails = this.companyLogoDetailsRepository.findByType(logoType.name());
		if (companyLogoDetails == null) {
			companyLogoDetails = new CompanyLogoDetails();
			companyLogoDetails.setCreatedById(VIRTUAL_CARD);
			companyLogoDetails.setCreatedOn(LocalDateTime.now());
			companyLogoDetails.setType(logoType.name());
		}
		else {
			companyLogoDetails.setUpdatedById(VIRTUAL_CARD);
			companyLogoDetails.setLastUpdate(LocalDateTime.now());
		}
		companyLogoDetails.setLogoUrl(sasURL);
		this.companyLogoDetailsRepository.save(companyLogoDetails);
	}

	private String generateSasUrl(BlobContainerClient containerClient, String blobName) {
		OffsetDateTime expiryTime = OffsetDateTime.now().plus(Duration.ofDays(1));
		BlobSasPermission permission = new BlobSasPermission().setReadPermission(true);

		BlobServiceSasSignatureValues values = new BlobServiceSasSignatureValues(expiryTime, permission)
			.setStartTime(OffsetDateTime.now())
			.setContentType("application/octet-stream");

		String sasToken = containerClient.getBlobClient(blobName).generateSas(values);
		return containerClient.getBlobClient(blobName).getBlobUrl() + "?" + sasToken;
	}

	@NotNull
	private static AcquirerAddress getShippingAddress(
			com.uhg.optum.financials.payments.payer.model.Address shippingAddres) {
		AcquirerAddress acquirerAddress = new AcquirerAddress();

		acquirerAddress.setLine1(shippingAddres.getLine1());
		acquirerAddress.setLine2(shippingAddres.getLine2());
		acquirerAddress.setCity(shippingAddres.getCity());
		acquirerAddress.setState(shippingAddres.getState());
		acquirerAddress.setZipCode(String.valueOf(shippingAddres.getPostalCode()));
		acquirerAddress.setCountry(shippingAddres.getCountry());
		return acquirerAddress;
	}

	private void sendEventToCDE(DocumentRequest documentRequest, UUID cardGuid, String paymentId,
			String issuerPaymentId, String orgShortCode, CardDetailsDTO cardDetailsDTO, String networkPaymentId) {
		DocumentRequest cdeDocumentRequest = DocumentRequest.copyOf(documentRequest);
		cdeDocumentRequest.setIsPCI(true);
		DocumentRequestCDE documentRequestCDE = new DocumentRequestCDE();
		documentRequestCDE.setDocumentRequest(cdeDocumentRequest);
		documentRequestCDE.setCguid(cardGuid.toString());
		documentRequestCDE.setPaymentId(paymentId);
		documentRequestCDE.setNetworkPaymentId(networkPaymentId);
		documentRequestCDE.setOrgShortCode(orgShortCode);
		documentRequestCDE.setBin(cardDetailsDTO.getBinRange().getBin());
		documentRequestCDE.setMaskedCardNumber(cardDetailsDTO.getMaskedPan());
		documentRequestCDE.setIssuerPaymentId(issuerPaymentId);
		EventBusEvent<DocumentRequestCDE> eventBusEvent = new EventBusEvent<>();
		eventBusEvent.setPayload(documentRequestCDE);
		eventBusEvent.setEventId(UUID.randomUUID());
		eventBusEvent.setTenantId(UUID.randomUUID().toString());
		eventBusEvent.setId(UUID.randomUUID().toString());
		eventBusEvent.setType(EventTypeMapping.UNMASKED_SUMMARY_PAGE_EVENT.getEventType());
		eventBusEvent.setSource(VIRTUAL_CARD_NON_CDE_SERVICE);
		eventBusEvent.setEntityName(VIRTUAL_CARD_NON_CDE_SERVICE);
		eventBusEvent.setVersion(1);
		eventBusEvent.setRetention(RetentionType.Persisted);
		eventBusEvent.setCreatedOn(Instant.now());
		this.logUtility.logMessage("Publishing event to event bus: " + eventBusEvent.getType(), INFO, log);
		this.eventBusProducer.sendEvent(eventBusEvent, null);

	}

	public DocumentCustomRemarks getCustomRemarks(String orgShortCode, String accountShortCode,
			String subAccountShortCode) {
		DocumentCustomRemarks documentCustomRemarks = this.documentCustomRemarksRepository
			.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode(orgShortCode, accountShortCode,
					subAccountShortCode);
		if (documentCustomRemarks == null) {
			documentCustomRemarks = this.documentCustomRemarksRepository
				.findByOrgShortCodeIsNullAndAccountShortCodeIsNullAndSubAccountShortCodeIsNull();
		}
		return documentCustomRemarks;
	}

}
