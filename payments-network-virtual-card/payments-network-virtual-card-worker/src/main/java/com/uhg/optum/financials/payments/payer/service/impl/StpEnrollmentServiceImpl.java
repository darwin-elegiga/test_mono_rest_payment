package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackEnrollmentErrorDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentCallbackHeaderDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentHeaderDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentRecordDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpEnrollmentValidationErrorDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpPartnerProviderDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpProviderEnrollmentRequestDto;
import com.uhg.optum.financials.payments.payer.model.StpEnrollmentBatch;
import com.uhg.optum.financials.payments.payer.model.StpEnrollmentRecord;
import com.uhg.optum.financials.payments.payer.repository.StpEnrollmentBatchRepository;
import com.uhg.optum.financials.payments.payer.repository.StpEnrollmentRecordRepository;
import com.uhg.optum.financials.payments.payer.service.StpEnrollmentOutboundClient;
import com.uhg.optum.financials.payments.payer.service.StpEnrollmentService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.core.env.Environment;
import org.springframework.stereotype.Service;
import org.springframework.util.StringUtils;

import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import java.util.regex.Pattern;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.INFO;

@Service
@Slf4j
public class StpEnrollmentServiceImpl implements StpEnrollmentService {

	private static final String BATCH_STATUS_RECEIVED = "RECEIVED";

	private static final String BATCH_STATUS_PROCESSED = "PROCESSED";

	private static final String BATCH_STATUS_PARTIAL_SUCCESS = "PARTIAL_SUCCESS";

	private static final String BATCH_STATUS_FAILED = "FAILED";

	private static final String CALLBACK_STATUS_SENT = "SENT";

	private static final String CALLBACK_STATUS_FAILED = "FAILED";

	private static final String CALLBACK_STATUS_SKIPPED = "SKIPPED";

	private static final String CALLBACK_STATUS_NOT_REQUIRED = "NOT_REQUIRED";

	private static final String PREFERENCING_STATUS_SENT = "SENT";

	private static final String PREFERENCING_STATUS_FAILED = "FAILED";

	private static final String PREFERENCING_STATUS_SKIPPED = "SKIPPED";

	private static final String RECORD_STATUS_SAVED = "SAVED";

	private static final String RECORD_STATUS_PREFERENCED = "PREFERENCED";

	private static final String ENROLLMENT_TYPE_C2B = "C2B";

	private static final String ENROLLMENT_TYPE_B2B = "B2B";

	private static final String ENROLLMENT_ELIGIBILITY_STATUS_Y = "Y";

	private static final String ENROLLMENT_ELIGIBILITY_STATUS_N = "N";

	private static final int MAX_CORRELATION_ID_LENGTH = 100;

	private static final int MAX_NETWORK_PARTNER_LENGTH = 100;

	private static final int MAX_PROVIDER_NAME_LENGTH = 255;

	private static final int MAX_ADDRESS_LENGTH = 255;

	private static final int MAX_CITY_LENGTH = 100;

	private static final Pattern TIN_PATTERN = Pattern.compile("^\\d{9}$");

	private static final Pattern NPI_PATTERN = Pattern.compile("^\\d{10}$");

	private static final Pattern ZIP_PATTERN = Pattern.compile("^(\\d{5}|\\d{9}|\\d{5}-\\d{4})$");

	private static final String ENROLLMENT_PROVIDER_CONFIG_PREFIX = "stp.enrollment.providers.";

	private final StpEnrollmentBatchRepository stpEnrollmentBatchRepository;

	private final StpEnrollmentRecordRepository stpEnrollmentRecordRepository;

	private final StpEnrollmentOutboundClient stpEnrollmentOutboundClient;

	private final Environment environment;

	private final LogUtility logUtility;

	@Value("${stp.enrollment.callback-url:}")
	private String callbackUrl;

	@Value("${stp.enrollment.preferencing-url:}")
	private String preferencingUrl;

	@Value("${stp.enrollment.recognized-partners:STP Partner}")
	private String recognizedPartners;

	public StpEnrollmentServiceImpl(StpEnrollmentBatchRepository stpEnrollmentBatchRepository,
			StpEnrollmentRecordRepository stpEnrollmentRecordRepository,
			StpEnrollmentOutboundClient stpEnrollmentOutboundClient, Environment environment, LogUtility logUtility) {
		this.stpEnrollmentBatchRepository = stpEnrollmentBatchRepository;
		this.stpEnrollmentRecordRepository = stpEnrollmentRecordRepository;
		this.stpEnrollmentOutboundClient = stpEnrollmentOutboundClient;
		this.environment = environment;
		this.logUtility = logUtility;
	}

	@Override
	public void processEnrollmentRequest(StpProviderEnrollmentRequestDto request) {

		String incomingCorrelationId = getCorrelationId(request);
		String storageCorrelationId = resolveStorageCorrelationId(incomingCorrelationId);
		if (this.stpEnrollmentBatchRepository.existsByCorrelationId(storageCorrelationId)) {
			this.logUtility.logMessage("[correlationId=" + storageCorrelationId
					+ "] Duplicate STP enrollment request skipped for idempotency", INFO, log);
			return;
		}

		List<StpEnrollmentValidationErrorDto> validationErrors = new ArrayList<>();
		List<ValidatedEnrollment> validEnrollments = new ArrayList<>();
		Set<String> requestKeys = new LinkedHashSet<>();

		validateHeader(request, validationErrors);
		StpEnrollmentBatch batch = canPersistBatch(request) ? createAndSaveBatch(request, storageCorrelationId) : null;
		validateProviders(request, validationErrors, validEnrollments, requestKeys);

		List<ValidatedEnrollment> processableValidEnrollments = (batch != null) ? validEnrollments : List.of();
		persistValidEnrollments(batch, processableValidEnrollments);

		String callbackStatus = handleCallbackIfNeeded(request, validationErrors);
		String preferencingStatus = handlePreferencingIfNeeded(request, processableValidEnrollments);

		updateRecordStatuses(processableValidEnrollments, preferencingStatus);
		updateBatchIfPresent(batch, processableValidEnrollments.size(), validationErrors.size(), callbackStatus,
				preferencingStatus);
	}

	private StpEnrollmentBatch createAndSaveBatch(StpProviderEnrollmentRequestDto request,
			String storageCorrelationId) {
		StpEnrollmentBatch batch = createBatch(request, storageCorrelationId);
		return this.stpEnrollmentBatchRepository.save(batch);
	}

	private StpEnrollmentBatch createBatch(StpProviderEnrollmentRequestDto request, String storageCorrelationId) {
		StpEnrollmentBatch batch = new StpEnrollmentBatch();
		batch.setCorrelationId(storageCorrelationId);
		batch.setNetworkPartner(getNetworkPartner(request));
		batch.setEnrollmentCount(getEnrollmentCount(request));
		batch.setValidCount(0);
		batch.setErrorCount(0);
		batch.setStatus(BATCH_STATUS_RECEIVED);
		batch.setCreatedAt(LocalDateTime.now());
		batch.setUpdatedAt(LocalDateTime.now());
		return batch;
	}

	private void validateHeader(StpProviderEnrollmentRequestDto request,
			List<StpEnrollmentValidationErrorDto> validationErrors) {
		if (request == null || request.getHeader() == null) {
			validationErrors.add(error(null, null, null, "header", "MISSING_HEADER", "Header is required"));
			return;
		}

		String networkPartner = normalize(request.getHeader().getNetworkPartner());
		String correlationId = normalize(request.getHeader().getCorrelationId());
		Integer count = request.getHeader().getCount();

		if (!StringUtils.hasText(networkPartner)) {
			validationErrors.add(error(null, null, null, "header.networkPartner", "MISSING_NETWORK_PARTNER",
					"networkPartner must be present"));
		}
		else if (isTooLong(networkPartner, MAX_NETWORK_PARTNER_LENGTH)) {
			validationErrors.add(error(null, null, null, "header.networkPartner", "INVALID_NETWORK_PARTNER_LENGTH",
					"networkPartner must not exceed 100 characters"));
		}
		else if (!isRecognizedPartner(networkPartner)) {
			validationErrors.add(error(null, null, null, "header.networkPartner", "INVALID_NETWORK_PARTNER",
					"networkPartner is not recognized"));
		}

		if (!StringUtils.hasText(correlationId)) {
			validationErrors.add(error(null, null, null, "header.correlationId", "MISSING_CORRELATION_ID",
					"correlationId must be present"));
		}
		else if (isTooLong(correlationId, MAX_CORRELATION_ID_LENGTH)) {
			validationErrors.add(error(null, null, null, "header.correlationId", "INVALID_CORRELATION_ID_LENGTH",
					"correlationId must not exceed 100 characters"));
		}

		if (count != null && count.intValue() != getEnrollmentCount(request)) {
			validationErrors.add(error(null, null, null, "header.count", "COUNT_MISMATCH",
					"count must match the number of enrollment records"));
		}
	}

	private void validateProviders(StpProviderEnrollmentRequestDto request,
			List<StpEnrollmentValidationErrorDto> validationErrors, List<ValidatedEnrollment> validEnrollments,
			Set<String> requestKeys) {
		if (request == null || request.getPartnerProviders() == null || request.getPartnerProviders().isEmpty()) {
			validationErrors.add(error(null, null, null, "partnerProviders", "EMPTY_PARTNER_PROVIDERS",
					"partnerProviders must contain at least one provider"));
			return;
		}

		for (StpPartnerProviderDto provider : request.getPartnerProviders()) {
			validateProvider(provider, validationErrors, validEnrollments, requestKeys);
		}
	}

	private void validateProvider(StpPartnerProviderDto provider,
			List<StpEnrollmentValidationErrorDto> validationErrors, List<ValidatedEnrollment> validEnrollments,
			Set<String> requestKeys) {
		String providerTin = (provider != null) ? normalize(provider.getProviderTin()) : null;
		List<StpEnrollmentValidationErrorDto> providerErrors = new ArrayList<>();

		if (!StringUtils.hasText(providerTin)) {
			providerErrors.add(error(providerTin, null, null, "providerTin", "MISSING_PROVIDER_TIN",
					"providerTin must be present"));
		}
		else if (!TIN_PATTERN.matcher(providerTin).matches()) {
			providerErrors.add(error(providerTin, null, null, "providerTin", "INVALID_PROVIDER_TIN",
					"providerTin must be 9 digits"));
		}
		if (provider == null || !StringUtils.hasText(normalize(provider.getLegalEntityName()))) {
			providerErrors.add(error(providerTin, null, null, "legalEntityName", "MISSING_LEGAL_ENTITY_NAME",
					"legalEntityName must be present"));
		}
		else if (isTooLong(normalize(provider.getLegalEntityName()), MAX_PROVIDER_NAME_LENGTH)) {
			providerErrors.add(error(providerTin, null, null, "legalEntityName", "INVALID_LEGAL_ENTITY_NAME_LENGTH",
					"legalEntityName must not exceed 255 characters"));
		}
		if (provider == null || !StringUtils.hasText(normalize(provider.getDoingBusinessAs()))) {
			providerErrors.add(error(providerTin, null, null, "doingBusinessAs", "MISSING_DOING_BUSINESS_AS",
					"doingBusinessAs must be present"));
		}
		else if (isTooLong(normalize(provider.getDoingBusinessAs()), MAX_PROVIDER_NAME_LENGTH)) {
			providerErrors.add(error(providerTin, null, null, "doingBusinessAs", "INVALID_DOING_BUSINESS_AS_LENGTH",
					"doingBusinessAs must not exceed 255 characters"));
		}
		if (provider == null || provider.getEnrollmentList() == null || provider.getEnrollmentList().isEmpty()) {
			providerErrors.add(error(providerTin, null, null, "enrollmentList", "EMPTY_ENROLLMENT_LIST",
					"enrollmentList must contain at least one enrollment"));
		}

		if (!providerErrors.isEmpty()) {
			validationErrors.addAll(providerErrors);
			return;
		}

		for (StpEnrollmentRecordDto enrollment : provider.getEnrollmentList()) {
			validateEnrollment(provider, enrollment, validationErrors, validEnrollments, requestKeys);
		}
	}

	private void validateEnrollment(StpPartnerProviderDto provider, StpEnrollmentRecordDto enrollment,
			List<StpEnrollmentValidationErrorDto> validationErrors, List<ValidatedEnrollment> validEnrollments,
			Set<String> requestKeys) {
		if (enrollment == null) {
			validationErrors.add(error(normalize(provider.getProviderTin()), null, null, "enrollment",
					"MISSING_ENROLLMENT_RECORD", "enrollment record must be present"));
			return;
		}

		String providerTin = normalize(provider.getProviderTin());
		String enrollmentType = normalizeUpper(enrollment.getEnrollmentType());
		List<StpEnrollmentValidationErrorDto> errors = new ArrayList<>();
		String eligibilityStatus = normalizeUpper(enrollment.getEnrollmentEligibilityStatus());
		String enrollmentStatusCd = normalizeUpper(enrollment.getEnrollmentStatusCd());
		String state = normalizeUpper(enrollment.getState());
		String zip9 = normalize(enrollment.getZip9());
		String npi = normalize(enrollment.getNpi());
		String uuid = normalize(enrollment.getUuid());

		if (!StringUtils.hasText(enrollmentType)) {
			errors.add(error(providerTin, npi, enrollmentType, "enrollmentType", "MISSING_ENROLLMENT_TYPE",
					"enrollmentType must be present"));
		}
		else if (!isValidEnrollmentType(enrollmentType)) {
			errors.add(error(providerTin, npi, enrollmentType, "enrollmentType", "INVALID_ENROLLMENT_TYPE",
					"enrollmentType must be C2B or B2B"));
		}
		if (!StringUtils.hasText(eligibilityStatus)) {
			errors.add(error(providerTin, npi, enrollmentType, "enrollmentEligibilityStatus",
					"MISSING_ENROLLMENT_ELIGIBILITY_STATUS", "enrollmentEligibilityStatus must be present"));
		}
		else if (!isValidEnrollmentEligibilityStatus(eligibilityStatus)) {
			errors.add(error(providerTin, npi, enrollmentType, "enrollmentEligibilityStatus",
					"INVALID_ENROLLMENT_ELIGIBILITY_STATUS", "enrollmentEligibilityStatus must be Y or N"));
		}
		if (!StringUtils.hasText(enrollmentStatusCd)) {
			errors.add(error(providerTin, npi, enrollmentType, "enrollmentStatusCd", "MISSING_ENROLLMENT_STATUS_CD",
					"enrollmentStatusCd must be present"));
		}
		else if (!isAllowedValue(enrollmentStatusCd, "A", "I")) {
			errors.add(error(providerTin, npi, enrollmentType, "enrollmentStatusCd", "INVALID_ENROLLMENT_STATUS_CD",
					"enrollmentStatusCd must be A or I"));
		}
		if (!StringUtils.hasText(normalize(enrollment.getAddress1()))) {
			errors.add(error(providerTin, npi, enrollmentType, "address1", "MISSING_ADDRESS1",
					"address1 must be present"));
		}
		else if (isTooLong(normalize(enrollment.getAddress1()), MAX_ADDRESS_LENGTH)) {
			errors.add(error(providerTin, npi, enrollmentType, "address1", "INVALID_ADDRESS1_LENGTH",
					"address1 must not exceed 255 characters"));
		}
		if (StringUtils.hasText(normalize(enrollment.getAddress2()))
				&& isTooLong(normalize(enrollment.getAddress2()), MAX_ADDRESS_LENGTH)) {
			errors.add(error(providerTin, npi, enrollmentType, "address2", "INVALID_ADDRESS2_LENGTH",
					"address2 must not exceed 255 characters"));
		}
		if (!StringUtils.hasText(normalize(enrollment.getCity()))) {
			errors.add(error(providerTin, npi, enrollmentType, "city", "MISSING_CITY", "city must be present"));
		}
		else if (isTooLong(normalize(enrollment.getCity()), MAX_CITY_LENGTH)) {
			errors.add(error(providerTin, npi, enrollmentType, "city", "INVALID_CITY_LENGTH",
					"city must not exceed 100 characters"));
		}
		if (!StringUtils.hasText(state)) {
			errors.add(error(providerTin, npi, enrollmentType, "state", "MISSING_STATE", "state must be present"));
		}
		else if (!Pattern.matches("^[A-Z]{2}$", state)) {
			errors.add(error(providerTin, npi, enrollmentType, "state", "INVALID_STATE",
					"state must be a valid 2-character code"));
		}
		if (!StringUtils.hasText(zip9)) {
			errors.add(error(providerTin, npi, enrollmentType, "zip9", "MISSING_ZIP", "zip9 must be present"));
		}
		else if (!ZIP_PATTERN.matcher(zip9).matches()) {
			errors.add(error(providerTin, npi, enrollmentType, "zip9", "INVALID_ZIP",
					"zip9 must be a valid ZIP or ZIP+4 value"));
		}
		if (StringUtils.hasText(npi) && !NPI_PATTERN.matcher(npi).matches()) {
			errors.add(error(providerTin, npi, enrollmentType, "npi", "INVALID_NPI",
					"npi must be 10 digits when provided"));
		}
		if (StringUtils.hasText(uuid) && !isValidUuidLength(uuid)) {
			errors.add(error(providerTin, npi, enrollmentType, "uuid", "INVALID_UUID_LENGTH",
					"uuid must be between 10 and 50 characters when provided"));
		}

		LocalDate effectiveDate = parseLocalDate(enrollment.getEffectiveDate(), "effectiveDate", providerTin, npi,
				enrollmentType, true, errors);
		LocalDate initialEnrollmentDate = parseLocalDate(enrollment.getInitialEnrollmentDate(), "initialEnrollmentDate",
				providerTin, npi, enrollmentType, true, errors);
		OffsetDateTime lastUpdatedUtcDate = parseOffsetDateTime(enrollment.getLastUpdatedUTCDate(),
				"lastUpdatedUTCDate", providerTin, npi, enrollmentType, errors);

		String requestKey = providerTin + "|" + enrollmentType;
		if (StringUtils.hasText(enrollmentType) && !requestKeys.add(requestKey)) {
			errors.add(error(providerTin, npi, enrollmentType, "enrollmentType+providerTin", "DUPLICATE_IN_REQUEST",
					"Duplicate enrollment found for providerTin and enrollmentType in the request"));
		}
		if (StringUtils.hasText(enrollmentType) && this.stpEnrollmentRecordRepository
			.existsByProviderTinAndEnrollmentType(providerTin, enrollmentType)) {
			errors.add(error(providerTin, npi, enrollmentType, "enrollmentType+providerTin", "DUPLICATE_ENROLLMENT",
					"Enrollment already exists for providerTin and enrollmentType"));
		}

		if (!errors.isEmpty()) {
			validationErrors.addAll(errors);
			return;
		}

		StpEnrollmentRecord record = new StpEnrollmentRecord();
		record.setProviderTin(providerTin);
		record.setLegalEntityName(normalize(provider.getLegalEntityName()));
		record.setDoingBusinessAs(normalize(provider.getDoingBusinessAs()));
		record.setNpi(npi);
		record.setProviderUuid(uuid);
		record.setAddress1(normalize(enrollment.getAddress1()));
		record.setAddress2(normalize(enrollment.getAddress2()));
		record.setCity(normalize(enrollment.getCity()));
		record.setState(state);
		record.setZip9(zip9);
		record.setEnrollmentType(enrollmentType);
		record.setEnrollmentEligibilityStatus(eligibilityStatus);
		record.setEffectiveDate(effectiveDate);
		record.setInitialEnrollmentDate(initialEnrollmentDate);
		record.setLastUpdatedUtcDate(lastUpdatedUtcDate);
		record.setEnrollmentStatusCd(enrollmentStatusCd);
		record.setEnrollmentStatusReason(normalize(enrollment.getEnrollmentStatusReason()));
		record.setStatus(RECORD_STATUS_SAVED);
		record.setCreatedAt(LocalDateTime.now());
		record.setUpdatedAt(LocalDateTime.now());
		validEnrollments.add(new ValidatedEnrollment(provider, enrollment, record));
	}

	private void persistValidEnrollments(StpEnrollmentBatch batch, List<ValidatedEnrollment> validEnrollments) {
		if (batch == null || validEnrollments.isEmpty()) {
			return;
		}
		List<StpEnrollmentRecord> records = validEnrollments.stream().map(ValidatedEnrollment::record).toList();
		records.forEach((record) -> record.setStpEnrollmentBatchId(batch.getId()));
		this.stpEnrollmentRecordRepository.saveAll(records);
		this.logUtility.logMessage(
				"[correlationId=" + batch.getCorrelationId() + "] Saved " + records.size() + " STP enrollment records",
				INFO, log);
	}

	private String handleCallbackIfNeeded(StpProviderEnrollmentRequestDto request,
			List<StpEnrollmentValidationErrorDto> validationErrors) {
		if (validationErrors.isEmpty()) {
			return CALLBACK_STATUS_NOT_REQUIRED;
		}
		String networkPartner = getNetworkPartner(request);
		String dynamicCallbackUrl = resolveCallbackUrl(networkPartner);
		if (!StringUtils.hasText(dynamicCallbackUrl)) {
			this.logUtility.logMessage("STP enrollment callback skipped because callback URL is not configured", INFO,
					log);
			return CALLBACK_STATUS_SKIPPED;
		}

		StpEnrollmentCallbackDto callbackDto = new StpEnrollmentCallbackDto();
		StpEnrollmentCallbackHeaderDto header = new StpEnrollmentCallbackHeaderDto();
		header.setCallbackUrl(dynamicCallbackUrl);
		header.setCorrelationId(getCorrelationId(request));
		header.setNetworkPartner(networkPartner);
		header.setStatus("Error");
		callbackDto.setHeader(header);
		callbackDto.setEnrollmentErrors(buildEnrollmentErrors(validationErrors));

		return (this.stpEnrollmentOutboundClient.sendValidationCallback(callbackDto, dynamicCallbackUrl))
				? CALLBACK_STATUS_SENT : CALLBACK_STATUS_FAILED;
	}

	// Config-driven: prefers explicit provider-specific callback URLs and falls back
	// to the legacy static stp.enrollment.callback-url only when it is configured.
	private String resolveCallbackUrl(String networkPartner) {
		String providerSpecificCallbackUrl = resolveProviderSpecificCallbackUrl(networkPartner);
		if (StringUtils.hasText(providerSpecificCallbackUrl)) {
			return providerSpecificCallbackUrl;
		}
		if (!StringUtils.hasText(this.callbackUrl)) {
			return null;
		}
		return StringUtils.hasText(networkPartner) ? this.callbackUrl + "/" + networkPartner : this.callbackUrl;
	}

	private String resolveProviderSpecificCallbackUrl(String networkPartner) {
		if (this.environment == null || !StringUtils.hasText(networkPartner)) {
			return null;
		}
		String providerKey = toPropertyKey(networkPartner);
		return normalize(
				this.environment.getProperty(ENROLLMENT_PROVIDER_CONFIG_PREFIX + providerKey + ".callback-url"));
	}

	private String toPropertyKey(String providerKey) {
		String normalized = normalize(providerKey);
		if (!StringUtils.hasText(normalized)) {
			return "default";
		}
		return normalized.toLowerCase(Locale.ROOT).replace(" ", "-");
	}

	private List<StpEnrollmentCallbackEnrollmentErrorDto> buildEnrollmentErrors(
			List<StpEnrollmentValidationErrorDto> validationErrors) {
		if (validationErrors.isEmpty()) {
			return null;
		}

		Map<String, List<StpEnrollmentValidationErrorDto>> groupedErrors = new LinkedHashMap<>();
		for (StpEnrollmentValidationErrorDto validationError : validationErrors) {
			groupedErrors.computeIfAbsent(buildEnrollmentErrorKey(validationError), (ignored) -> new ArrayList<>())
				.add(validationError);
		}

		List<StpEnrollmentCallbackEnrollmentErrorDto> enrollmentErrors = new ArrayList<>();
		for (List<StpEnrollmentValidationErrorDto> groupedValidationErrors : groupedErrors.values()) {
			StpEnrollmentValidationErrorDto firstError = groupedValidationErrors.getFirst();
			StpEnrollmentCallbackEnrollmentErrorDto enrollmentError = new StpEnrollmentCallbackEnrollmentErrorDto();
			enrollmentError.setProviderTin(firstError.getProviderTin());
			enrollmentError.setNpi(firstError.getNpi());
			enrollmentError.setEnrollmentType(firstError.getEnrollmentType());
			enrollmentError.setErrors(groupedValidationErrors.stream()
				.map(StpEnrollmentValidationErrorDto::getMessage)
				.distinct()
				.toList());
			enrollmentErrors.add(enrollmentError);
		}
		return enrollmentErrors;
	}

	private String buildEnrollmentErrorKey(StpEnrollmentValidationErrorDto validationError) {
		return normalize(validationError.getProviderTin()) + "|" + normalize(validationError.getNpi()) + "|"
				+ normalize(validationError.getEnrollmentType());
	}

	private String handlePreferencingIfNeeded(StpProviderEnrollmentRequestDto request,
			List<ValidatedEnrollment> validEnrollments) {
		if (validEnrollments.isEmpty()) {
			return PREFERENCING_STATUS_SKIPPED;
		}
		if (!StringUtils.hasText(this.preferencingUrl)) {
			this.logUtility.logMessage("STP enrollment preferencing handoff skipped because endpoint is not configured",
					INFO, log);
			return PREFERENCING_STATUS_SKIPPED;
		}

		StpProviderEnrollmentRequestDto filteredRequest = buildPreferencingRequest(request, validEnrollments);
		return (this.stpEnrollmentOutboundClient.sendToPreferencing(filteredRequest, this.preferencingUrl))
				? PREFERENCING_STATUS_SENT : PREFERENCING_STATUS_FAILED;
	}

	private StpProviderEnrollmentRequestDto buildPreferencingRequest(StpProviderEnrollmentRequestDto request,
			List<ValidatedEnrollment> validEnrollments) {
		StpProviderEnrollmentRequestDto filteredRequest = new StpProviderEnrollmentRequestDto();

		StpEnrollmentHeaderDto header = new StpEnrollmentHeaderDto();
		header.setCorrelationId(getCorrelationId(request));
		header.setNetworkPartner(getNetworkPartner(request));
		header.setCount(validEnrollments.size());
		filteredRequest.setHeader(header);

		List<StpPartnerProviderDto> partnerProviders = new ArrayList<>();
		for (ValidatedEnrollment validEnrollment : validEnrollments) {
			StpPartnerProviderDto existingProvider = partnerProviders.stream()
				.filter((provider) -> normalize(provider.getProviderTin())
					.equals(normalize(validEnrollment.provider().getProviderTin())))
				.findFirst()
				.orElse(null);
			if (existingProvider == null) {
				existingProvider = new StpPartnerProviderDto();
				existingProvider.setProviderTin(validEnrollment.provider().getProviderTin());
				existingProvider.setLegalEntityName(validEnrollment.provider().getLegalEntityName());
				existingProvider.setDoingBusinessAs(validEnrollment.provider().getDoingBusinessAs());
				existingProvider.setEnrollmentList(new ArrayList<>());
				partnerProviders.add(existingProvider);
			}
			existingProvider.getEnrollmentList().add(validEnrollment.enrollment());
		}
		filteredRequest.setPartnerProviders(partnerProviders);
		return filteredRequest;
	}

	private void updateRecordStatuses(List<ValidatedEnrollment> validEnrollments, String preferencingStatus) {
		if (!PREFERENCING_STATUS_SENT.equals(preferencingStatus)) {
			return;
		}
		List<StpEnrollmentRecord> records = validEnrollments.stream().map(ValidatedEnrollment::record).toList();
		records.forEach((record) -> {
			record.setStatus(RECORD_STATUS_PREFERENCED);
			record.setUpdatedAt(LocalDateTime.now());
		});
		this.stpEnrollmentRecordRepository.saveAll(records);
	}

	private void updateBatchIfPresent(StpEnrollmentBatch batch, int validCount, int errorCount, String callbackStatus,
			String preferencingStatus) {
		if (batch == null) {
			return;
		}
		batch.setValidCount(validCount);
		batch.setErrorCount(errorCount);
		batch.setCallbackStatus(callbackStatus);
		batch.setPreferencingStatus(preferencingStatus);
		batch.setStatus(resolveBatchStatus(validCount, errorCount));
		batch.setUpdatedAt(LocalDateTime.now());
		this.stpEnrollmentBatchRepository.save(batch);
	}

	private String resolveBatchStatus(int validCount, int errorCount) {
		if (validCount > 0 && errorCount > 0) {
			return BATCH_STATUS_PARTIAL_SUCCESS;
		}
		if (validCount > 0) {
			return BATCH_STATUS_PROCESSED;
		}
		return BATCH_STATUS_FAILED;
	}

	private boolean isRecognizedPartner(String networkPartner) {
		String normalizedPartner = normalize(networkPartner);
		for (String configuredPartner : this.recognizedPartners.split(",")) {
			if (normalizedPartner.equalsIgnoreCase(normalize(configuredPartner))) {
				return true;
			}
		}
		return false;
	}

	private String resolveStorageCorrelationId(String correlationId) {
		if (StringUtils.hasText(correlationId)) {
			return correlationId.trim();
		}
		return "missing-correlation-" + UUID.randomUUID();
	}

	private boolean canPersistBatch(StpProviderEnrollmentRequestDto request) {
		if (request == null || request.getHeader() == null) {
			return false;
		}
		String correlationId = normalize(request.getHeader().getCorrelationId());
		String networkPartner = normalize(request.getHeader().getNetworkPartner());
		return !isTooLong(correlationId, MAX_CORRELATION_ID_LENGTH)
				&& !isTooLong(networkPartner, MAX_NETWORK_PARTNER_LENGTH);
	}

	private LocalDate parseLocalDate(String value, String field, String providerTin, String npi, String enrollmentType,
			boolean required, List<StpEnrollmentValidationErrorDto> errors) {
		if (!StringUtils.hasText(normalize(value))) {
			if (required) {
				errors.add(error(providerTin, npi, enrollmentType, field, "MISSING_" + field.toUpperCase(Locale.ROOT),
						field + " must be present"));
			}
			return null;
		}
		try {
			return LocalDate.parse(value);
		}
		catch (DateTimeParseException ex) {
			errors.add(error(providerTin, npi, enrollmentType, field, "INVALID_" + field.toUpperCase(Locale.ROOT),
					field + " must be a valid date"));
			return null;
		}
	}

	private OffsetDateTime parseOffsetDateTime(String value, String field, String providerTin, String npi,
			String enrollmentType, List<StpEnrollmentValidationErrorDto> errors) {
		if (!StringUtils.hasText(normalize(value))) {
			errors.add(error(providerTin, npi, enrollmentType, field, "MISSING_" + field.toUpperCase(Locale.ROOT),
					field + " must be present"));
			return null;
		}
		try {
			return OffsetDateTime.parse(value);
		}
		catch (DateTimeParseException ex) {
			errors.add(error(providerTin, npi, enrollmentType, field, "INVALID_" + field.toUpperCase(Locale.ROOT),
					field + " must be a valid UTC timestamp"));
			return null;
		}
	}

	private boolean isAllowedValue(String actual, String... expectedValues) {
		if (!StringUtils.hasText(actual)) {
			return false;
		}
		for (String expectedValue : expectedValues) {
			if (expectedValue.equalsIgnoreCase(actual)) {
				return true;
			}
		}
		return false;
	}

	private boolean isValidEnrollmentType(String enrollmentType) {
		return isAllowedValue(enrollmentType, ENROLLMENT_TYPE_C2B, ENROLLMENT_TYPE_B2B);
	}

	private boolean isValidEnrollmentEligibilityStatus(String eligibilityStatus) {
		return isAllowedValue(eligibilityStatus, ENROLLMENT_ELIGIBILITY_STATUS_Y, ENROLLMENT_ELIGIBILITY_STATUS_N);
	}

	private boolean isValidUuidLength(String value) {
		String normalized = normalize(value);
		return normalized != null && normalized.length() >= 10 && normalized.length() <= 50;
	}

	private boolean isTooLong(String value, int maxLength) {
		String normalized = normalize(value);
		return normalized != null && normalized.length() > maxLength;
	}

	private String getCorrelationId(StpProviderEnrollmentRequestDto request) {
		return (request != null && request.getHeader() != null) ? normalize(request.getHeader().getCorrelationId())
				: null;
	}

	private String getNetworkPartner(StpProviderEnrollmentRequestDto request) {
		return (request != null && request.getHeader() != null) ? normalize(request.getHeader().getNetworkPartner())
				: null;
	}

	private int getEnrollmentCount(StpProviderEnrollmentRequestDto request) {
		if (request == null || request.getPartnerProviders() == null) {
			return 0;
		}
		return request.getPartnerProviders()
			.stream()
			.mapToInt((provider) -> (provider.getEnrollmentList() != null) ? provider.getEnrollmentList().size() : 0)
			.sum();
	}

	private String normalize(String value) {
		return (value != null) ? value.trim() : null;
	}

	private String normalizeUpper(String value) {
		String normalized = normalize(value);
		return (normalized != null) ? normalized.toUpperCase(Locale.ROOT) : null;
	}

	private StpEnrollmentValidationErrorDto error(String providerTin, String npi, String enrollmentType, String field,
			String code, String message) {
		StpEnrollmentValidationErrorDto dto = new StpEnrollmentValidationErrorDto();
		dto.setProviderTin(providerTin);
		dto.setNpi(npi);
		dto.setEnrollmentType(enrollmentType);
		dto.setField(field);
		dto.setCode(code);
		dto.setMessage(message);
		return dto;
	}

	private record ValidatedEnrollment(StpPartnerProviderDto provider, StpEnrollmentRecordDto enrollment,
			StpEnrollmentRecord record) {
	}

}
