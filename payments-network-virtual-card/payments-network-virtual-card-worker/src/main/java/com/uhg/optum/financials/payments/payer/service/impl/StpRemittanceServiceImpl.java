package com.uhg.optum.financials.payments.payer.service.impl;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.stp.StpConsumerDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpPayerDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpPayeeDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpRequestHeaderDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpVirtualCardRequestDto;
import com.uhg.optum.financials.payments.payer.enums.PayeeType;
import com.uhg.optum.financials.payments.payer.model.Address;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.StpEnrollmentBatch;
import com.uhg.optum.financials.payments.payer.model.StpEnrollmentRecord;
import com.uhg.optum.financials.payments.payer.model.SubAccount;
import com.uhg.optum.financials.payments.payer.repository.StpEnrollmentBatchRepository;
import com.uhg.optum.financials.payments.payer.repository.StpEnrollmentRecordRepository;
import com.uhg.optum.financials.payments.payer.repository.SubAccountRepository;
import com.uhg.optum.financials.payments.payer.service.StpProviderClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClient;
import com.uhg.optum.financials.shared.core.http.resilience.ResilientRestClientFactory;
import com.uhg.optum.financials.payments.payer.service.StpRemittanceService;
import com.uhg.optum.financials.payments.payer.util.PaymentTypeCategoryUtil;
import lombok.extern.slf4j.Slf4j;
import org.springframework.core.env.Environment;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;
import org.springframework.web.client.RestClientException;

import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;
import java.util.Comparator;
import java.util.Locale;
import java.util.Optional;

@Service
@Slf4j
public class StpRemittanceServiceImpl implements StpRemittanceService {

	public static final String TRN_02_S_S_INFO = "TRN*02*%s*%s*INFO~";

	private static final String ELIGIBILITY_Y = "Y";

	private static final String ENROLLMENT_STATUS_ACTIVE = "A";

	private static final String PATIENT_ENTITY_CODE = "QC";

	private static final String PATIENT_ENTITY_TYPE_QUALIFIER = "1";

	private static final String PROVIDER_ENTITY_CODE = "82";

	private static final String PROVIDER_ENTITY_TYPE_QUALIFIER = "1";

	private static final String PROVIDER_CONFIG_PREFIX = "stp.remittance.providers.";

	private final StpEnrollmentRecordRepository stpEnrollmentRecordRepository;

	private final StpEnrollmentBatchRepository stpEnrollmentBatchRepository;

	private final Environment environment;

	private final ObjectMapper objectMapper;

	private final SubAccountRepository subAccountRepository;

	private final StpProviderClient stpProviderClient;

	private final ResilientRestClient claimClient;

	public StpRemittanceServiceImpl(StpEnrollmentRecordRepository stpEnrollmentRecordRepository,
			StpEnrollmentBatchRepository stpEnrollmentBatchRepository, Environment environment,
			ObjectMapper objectMapper, SubAccountRepository subAccountRepository, StpProviderClient stpProviderClient,
			ResilientRestClientFactory resilientRestClientFactory) {
		this.stpEnrollmentRecordRepository = stpEnrollmentRecordRepository;
		this.stpEnrollmentBatchRepository = stpEnrollmentBatchRepository;
		this.environment = environment;
		this.objectMapper = objectMapper;
		this.subAccountRepository = subAccountRepository;
		this.stpProviderClient = stpProviderClient;
		this.claimClient = resilientRestClientFactory.getClient("stp-claim");
	}

	@Override
	public void sendRemittance(PaymentRequest paymentRequest) {
		// Step 1: Validate Network payment ID exists
		String networkPaymentId = validateAndGetNetworkPaymentId(paymentRequest);
		if (!StringUtils.hasText(networkPaymentId)) {
			log.info("Skipping STP remittance: networkPaymentId is missing or invalid");
			return;
		}

		// Step 2: Resolve enrollment type from payee type
		PayeeType payeeType = resolvePayeeType(paymentRequest);
		if (payeeType == null) {
			log.info("Skipping STP remittance: unsupported payee type");
			return;
		}

		// Step 3: Check if payment is eligible for STP (verify enrollment table has data)
		String providerTin = normalize(
				(paymentRequest.getPayeeDetails() != null) ? paymentRequest.getPayeeDetails().getTaxId() : null);
		Optional<String> providerKeyOptional = isPaymentEligibleForStp(providerTin, payeeType);
		if (providerKeyOptional.isEmpty()) {
			log.warn("Skipping STP remittance: payment not eligible. providerTin={} payeeType={}", providerTin,
					payeeType);
			return;
		}

		String providerKey = providerKeyOptional.get();
		ProviderConfig providerConfig = getProviderConfig(providerKey);
		if (!StringUtils.hasText(providerConfig.endpoint())) {
			log.warn("Skipping STP remittance: endpoint not configured for provider key={}", providerKey);
			return;
		}

		// Step 4: Build the payload
		StpVirtualCardRequestDto payload = buildPayload(paymentRequest, providerKey, payeeType);
		try {
			log.debug("Prepared STP remittance header for provider key={} endpoint={} authType={} header={}",
					providerKey, providerConfig.endpoint(), providerConfig.authType(),
					this.objectMapper.writeValueAsString(payload.getHeader()));
		}
		catch (JsonProcessingException ex) {
			log.debug("Failed to serialize STP remittance header for provider key={}", providerKey, ex);
		}
		log.info("Prepared STP remittance request for provider key={} endpoint={} authType={} networkPaymentId={}",
				providerKey, providerConfig.endpoint(), providerConfig.authType(), networkPaymentId);

		// Step 5: Add OAuth request configuration and call the remittance endpoint
		this.stpProviderClient.postRemittance(providerKey,
				new StpProviderClient.ProviderConfig(providerConfig.endpoint(), providerConfig.authType()), payload,
				networkPaymentId, paymentRequest.getId());
	}

	/**
	 * Validates and retrieves the Network Payment ID from the payment request.
	 * @param paymentRequest the payment request
	 * @return the network payment ID if valid, null otherwise
	 */
	private String validateAndGetNetworkPaymentId(PaymentRequest paymentRequest) {
		if (paymentRequest == null || paymentRequest.getPaymentDetails() == null) {
			return null;
		}
		String networkPaymentId = normalize(paymentRequest.getPaymentDetails().getNetworkPaymentId());
		return StringUtils.hasText(networkPaymentId) ? networkPaymentId : null;
	}

	/**
	 * Resolves C2B/B2B from paymentDetails.type.
	 * @param paymentRequest the payment request
	 * @return the resolved PayeeType, defaulting to B2B when paymentDetails.type is null,
	 * blank, or not categorized as C2B; returns null only when paymentRequest is null
	 */
	private PayeeType resolvePayeeType(PaymentRequest paymentRequest) {
		if (paymentRequest == null) {
			return null;
		}
		String paymentType = (paymentRequest.getPaymentDetails() != null
				&& paymentRequest.getPaymentDetails().getType() != null)
						? paymentRequest.getPaymentDetails().getType().name() : null;
		return PayeeType.fromInput(PaymentTypeCategoryUtil.toC2bOrB2b(paymentType));
	}

	/**
	 * Checks if the payment is eligible for STP remittance by verifying enrollment
	 * records exist. This method queries the enrollment table for matching records with:
	 * - Provider TIN matching the payee's tax ID - Enrollment type matching the PayeeType
	 * - Eligibility status as 'Y' - Enrollment status as 'A' (Active)
	 * @param providerTin the provider's tax ID
	 * @param payeeType the payee type (B2B or C2B)
	 * @return optional containing provider key if eligible, empty otherwise
	 */
	private Optional<String> isPaymentEligibleForStp(String providerTin, PayeeType payeeType) {
		if (!StringUtils.hasText(providerTin)) {
			log.debug("Payment eligibility check failed: providerTin is empty");
			return Optional.empty();
		}

		return this.stpEnrollmentRecordRepository.findAll()
			.stream()
			.filter((record) -> providerTin.equalsIgnoreCase(normalize(record.getProviderTin())))
			.filter((record) -> payeeType.name().equalsIgnoreCase(normalize(record.getEnrollmentType())))
			.filter((record) -> ELIGIBILITY_Y.equalsIgnoreCase(normalize(record.getEnrollmentEligibilityStatus())))
			.filter((record) -> ENROLLMENT_STATUS_ACTIVE.equalsIgnoreCase(normalize(record.getEnrollmentStatusCd())))
			.sorted(Comparator.comparing(StpEnrollmentRecord::getUpdatedAt,
					Comparator.nullsLast(Comparator.reverseOrder())))
			.map(this::resolveNetworkPartner)
			.flatMap(Optional::stream)
			.findFirst();
	}

	private Optional<String> resolveNetworkPartner(StpEnrollmentRecord record) {
		if (record == null || record.getStpEnrollmentBatchId() == null) {
			return Optional.empty();
		}
		Optional<StpEnrollmentBatch> batch = this.stpEnrollmentBatchRepository
			.findById(record.getStpEnrollmentBatchId());
		return batch.map(StpEnrollmentBatch::getNetworkPartner).map(this::normalize).filter(StringUtils::hasText);
	}

	private ProviderConfig getProviderConfig(String providerKey) {
		String key = toPropertyKey(providerKey);
		String prefix = PROVIDER_CONFIG_PREFIX + key + ".";
		String endpoint = normalize(this.environment.getProperty(prefix + "endpoint"));
		String authType = normalize(this.environment.getProperty(prefix + "auth-type"));
		return new ProviderConfig(endpoint, defaultValue(authType, "none"));
	}

	private StpVirtualCardRequestDto buildPayload(PaymentRequest paymentRequest, String providerKey,
			PayeeType payeeType) {
		StpRequestHeaderDto header = new StpRequestHeaderDto();
		header.setNetworkPartner(providerKey);
		header.setNetworkPaymentId(paymentRequest.getPaymentDetails().getNetworkPaymentId());

		StpVirtualCardRequestDto requestDto = new StpVirtualCardRequestDto();
		requestDto.setHeader(header);
		if (PayeeType.C2B == payeeType) {
			requestDto.setConsumer(buildConsumer(paymentRequest));
		}

		StpPayeeDto payee = getStpPayeeDto(paymentRequest, payeeType);

		StpPayerDto payer = getStpPayerDto(paymentRequest, payeeType);

		requestDto.setPayee(payee);
		requestDto.setPayer(payer);
		return requestDto;
	}

	private StpPayerDto getStpPayerDto(PaymentRequest paymentRequest, PayeeType payeeType) {
		StpPayerDto payer = new StpPayerDto();

		SubAccount subAccount = this.subAccountRepository.findByOrgShortCodeAndSubAccountShortCodeAndAccountShortCode(
				paymentRequest.getOrgShortCode(), paymentRequest.getSubAccountShortCode(),
				paymentRequest.getAccountShortCode());
		if (subAccount == null) {
			return payer;
		}
		payer.setTraceNumber(resolveTraceNumber(paymentRequest, subAccount.getSubAccountShortCode()));
		if (PayeeType.B2B == payeeType) {
			payer.setName(subAccount.getName());
		}
		else {
			if (subAccount.getAddress() != null) {
				Address address = subAccount.getAddress();
				payer.setAddress(normalize(address.getLine1()));
				payer.setCity(normalize(address.getCity()));
				payer.setState(normalize(address.getState()));
				payer.setZip(normalize(address.getPostalCode()));
			}

			payer.setTpa(normalize(paymentRequest.getPayr835Id()));
		}

		return payer;
	}

	private StpPayeeDto getStpPayeeDto(PaymentRequest paymentRequest, PayeeType payeeType) {
		StpPayeeDto payee = new StpPayeeDto();
		payee.setName(normalize(paymentRequest.getPayeeDetails().getName()));
		payee.setTin(normalize(paymentRequest.getPayeeDetails().getTaxId()));
		if (PayeeType.C2B == payeeType) {
			payee.setUuid(normalize(paymentRequest.getPayeeDetails().getReceiverId()));
			if (paymentRequest.getPayeeDetails() != null && paymentRequest.getPayeeDetails().getAddress() != null) {
				payee.setAddressLine1(normalize(paymentRequest.getPayeeDetails().getAddress().getLine1()));
				payee.setCity(normalize(paymentRequest.getPayeeDetails().getAddress().getCity()));
				payee.setState(normalize(paymentRequest.getPayeeDetails().getAddress().getState()));
				payee.setZip(normalize(paymentRequest.getPayeeDetails().getAddress().getPostalCode()));
			}
		}
		return payee;
	}

	private StpConsumerDto buildConsumer(PaymentRequest paymentRequest) {
		JsonNode claimLoopNode = resolveClaimLoopNode(paymentRequest);
		JsonNode claimPaymentInformation = claimLoopNode.path("claimPaymentInformation");
		JsonNode patientEntity = resolveInvolvedEntity(claimLoopNode, PATIENT_ENTITY_CODE,
				PATIENT_ENTITY_TYPE_QUALIFIER);
		JsonNode providerEntity = resolveInvolvedEntity(claimLoopNode, PROVIDER_ENTITY_CODE,
				PROVIDER_ENTITY_TYPE_QUALIFIER);

		StpConsumerDto consumer = new StpConsumerDto();
		consumer.setPatientName(buildEntityName(patientEntity));
		consumer.setPatientDOB(formatDate(readText(patientEntity, "dob")));
		consumer.setClaimId(readText(claimPaymentInformation, "patientControlNumber"));
		consumer.setPatientAccountNumber(readText(patientEntity, "patientAccountNumber"));
		consumer.setAmount(readDecimal(claimPaymentInformation, "paymentAmount"));
		consumer.setClaimDate(formatDate(resolveClaimDate(claimLoopNode)));
		consumer.setSubscriberName(buildEntityName(providerEntity));
		consumer.setClientReferenceId(resolveClientReferenceId(paymentRequest));
		return consumer;
	}

	private JsonNode resolveClaimLoopNode(PaymentRequest paymentRequest) {
		JsonNode claimRoot = readClaimJson(paymentRequest);
		if (claimRoot == null || claimRoot.isNull() || claimRoot.isMissingNode()) {
			return this.objectMapper.createObjectNode();
		}

		JsonNode claimLevelNode = firstArrayElement(claimRoot.path("claimLevelInformation"));
		if (claimLevelNode != null) {
			JsonNode claimLoopNode = firstArrayElement(claimLevelNode.path("claimLoop"));
			if (claimLoopNode != null) {
				return claimLoopNode;
			}
		}

		JsonNode directClaimLoop = firstArrayElement(claimRoot.path("claimLoop"));
		if (directClaimLoop != null) {
			return directClaimLoop;
		}
		return claimRoot;
	}

	private JsonNode readClaimJson(PaymentRequest paymentRequest) {
		// Get claim JSON from clmUrl field only (no fallback to metadata)
		String clmUrl = normalize(paymentRequest.getClmUrl());
		if (!StringUtils.hasText(clmUrl)) {
			log.debug("Claim URL (clmUrl) is empty or null");
			return null;
		}

		// Try URL decoding if the value contains encoded characters
		String decodedUrl = decodeUrl(clmUrl);

		// Check if it's a valid HTTP(S) URL
		if (isHttpUrl(decodedUrl)) {
			return readClaimJsonFromUrl(decodedUrl, "clmUrl");
		}

		// If not a URL, try parsing as direct JSON
		return tryParseClaimJson(clmUrl, "clmUrl");
	}

	private JsonNode tryParseClaimJson(String value, String key) {
		try {
			return this.objectMapper.readTree(value);
		}
		catch (Exception ex) {
			log.debug("Unable to parse claim JSON from metadata key={}: {}", key, ex.getMessage());
			return null;
		}
	}

	private String decodeUrl(String value) {
		if (!StringUtils.hasText(value) || !value.contains("%")) {
			return value;
		}
		try {
			return URLDecoder.decode(value, StandardCharsets.UTF_8);
		}
		catch (IllegalArgumentException ex) {
			log.debug("Unable to URL decode claim metadata value: {}", ex.getMessage());
			return value;
		}
	}

	private boolean isHttpUrl(String value) {
		String normalized = normalize(value);
		if (!StringUtils.hasText(normalized)) {
			return false;
		}
		String lower = normalized.toLowerCase(Locale.ROOT);
		return lower.startsWith("https://") || lower.startsWith("http://");
	}

	private JsonNode readClaimJsonFromUrl(String claimUrl, String metadataKey) {
		try {
			String responseBody = this.claimClient
				.execute((client) -> client.get().uri(claimUrl).retrieve().body(String.class));

			if (!StringUtils.hasText(responseBody)) {
				log.debug("Empty response body from claim URL metadata key={} url={}", metadataKey, claimUrl);
				return null;
			}
			return tryParseClaimJson(responseBody, metadataKey);
		}
		catch (RestClientException ex) {
			log.debug("Unable to read claim JSON from url={}: {}", claimUrl, ex.getMessage());
			return null;
		}
		catch (Exception ex) {
			log.debug("Unexpected error reading claim JSON from url={}: {}", claimUrl, ex.getMessage());
			return null;
		}
	}

	private JsonNode resolveInvolvedEntity(JsonNode claimLoopNode, String entityIdentifierCode,
			String entityTypeQualifier) {
		if (claimLoopNode == null || claimLoopNode.isNull() || claimLoopNode.isMissingNode()) {
			return null;
		}
		JsonNode involvedEntities = claimLoopNode.path("involvedEntitiesInfo");
		if (!involvedEntities.isArray()) {
			return null;
		}
		for (JsonNode entity : involvedEntities) {
			if (entityIdentifierCode.equalsIgnoreCase(readText(entity, "entityIdentifierCode"))
					&& entityTypeQualifier.equalsIgnoreCase(readText(entity, "entityTypeQualifier"))) {
				return entity;
			}
		}
		return null;
	}

	private JsonNode firstArrayElement(JsonNode arrayNode) {
		if (arrayNode != null && arrayNode.isArray() && !arrayNode.isEmpty()) {
			return arrayNode.get(0);
		}
		return null;
	}

	private String resolveClaimDate(JsonNode claimLoopNode) {
		if (claimLoopNode == null || claimLoopNode.isNull() || claimLoopNode.isMissingNode()) {
			return null;
		}
		JsonNode loop2110 = claimLoopNode.path("loop2110");
		if (!loop2110.isArray()) {
			return null;
		}
		for (JsonNode serviceLoop : loop2110) {
			JsonNode serviceDates = serviceLoop.path("serviceDate");
			if (!serviceDates.isArray()) {
				continue;
			}
			for (JsonNode dateNode : serviceDates) {
				String qualifier = readText(dateNode, "dateTimeQualifier", "qualifier", "dtm01", "DTM01");
				String date = readText(dateNode, "date", "dtm02", "DTM02");
				if ("472".equals(qualifier) && StringUtils.hasText(date)) {
					return date;
				}
			}
		}
		return null;
	}

	private String buildEntityName(JsonNode entityNode) {
		if (entityNode == null || entityNode.isNull() || entityNode.isMissingNode()) {
			return null;
		}
		String firstName = readText(entityNode, "firstName");
		String lastName = readText(entityNode, "lastName");
		if (StringUtils.hasText(firstName) && StringUtils.hasText(lastName)) {
			return firstName + " " + lastName;
		}
		if (StringUtils.hasText(firstName)) {
			return firstName;
		}
		if (StringUtils.hasText(lastName)) {
			return lastName;
		}
		return null;
	}

	private String readText(JsonNode node, String... keys) {
		if (node == null || node.isNull() || node.isMissingNode() || keys == null) {
			return null;
		}
		for (String key : keys) {
			String value = normalize(node.path(key).asText(null));
			if (StringUtils.hasText(value)) {
				return value;
			}
		}
		return null;
	}

	private String readDecimal(JsonNode node, String key) {
		if (node == null || node.isNull() || node.isMissingNode()) {
			return null;
		}
		JsonNode valueNode = node.path(key);
		if (valueNode.isNumber() || valueNode.isTextual()) {
			String value = normalize(valueNode.asText());
			return StringUtils.hasText(value) ? value : null;
		}
		return null;
	}

	private String resolveTraceNumber(PaymentRequest paymentRequest, String subAcctShortCode) {
		String issuerPaymentId = normalize((paymentRequest.getPaymentDetails() != null)
				? paymentRequest.getPaymentDetails().getIssuerPaymentId() : null);
		return String.format(Locale.ROOT, TRN_02_S_S_INFO, defaultValue(issuerPaymentId, "UNKNOWN"),
				defaultValue(subAcctShortCode, "UN" + "KNOWN"));
	}

	private String resolveClientReferenceId(PaymentRequest paymentRequest) {
		if (paymentRequest.getPaymentDetails() != null) {
			String referenceId = normalize(paymentRequest.getPaymentDetails().getReferenceId());
			if (StringUtils.hasText(referenceId)) {
				return referenceId;
			}
		}
		return null;
	}

	private String formatDate(String rawDate) {
		String normalized = normalize(rawDate);
		if (!StringUtils.hasText(normalized)) {
			return null;
		}
		if (normalized.matches("\\d{8}")) {
			return normalized.substring(0, 4) + "-" + normalized.substring(4, 6) + "-" + normalized.substring(6, 8);
		}
		return normalized;
	}

	private String toPropertyKey(String providerKey) {
		String normalized = normalize(providerKey);
		if (!StringUtils.hasText(normalized)) {
			return "default";
		}
		return normalized.toLowerCase(Locale.ROOT).replace(" ", "-");
	}

	private String normalize(String value) {
		return (value != null) ? value.trim() : null;
	}

	private String defaultValue(String value, String fallback) {
		return StringUtils.hasText(value) ? value : fallback;
	}

	private record ProviderConfig(String endpoint, String authType) {
	}

}
