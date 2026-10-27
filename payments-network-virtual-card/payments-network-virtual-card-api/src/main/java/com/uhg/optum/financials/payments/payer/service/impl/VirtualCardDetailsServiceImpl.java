package com.uhg.optum.financials.payments.payer.service.impl;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.google.gson.Gson;
import com.uhg.optum.financials.payments.payer.constant.ApiConstants;
import com.uhg.optum.financials.payments.payer.dto.AuditLogDto;
import com.uhg.optum.financials.payments.payer.dto.CardCreationStatusDto;
import com.uhg.optum.financials.payments.payer.dto.CardDetailsDTO;
import com.uhg.optum.financials.payments.payer.dto.CardErrorResponseDto;
import com.uhg.optum.financials.payments.payer.dto.VirtualCardHistoryDto;
import com.uhg.optum.financials.payments.payer.enums.CardEventType;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.util.HttpExceptionUtil;
import com.uhg.optum.financials.payments.payer.model.AuditLog;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.ClearExtract;
import com.uhg.optum.financials.payments.payer.model.VirtualCardHistory;
import com.uhg.optum.financials.payments.payer.repository.AuditLogRepository;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.ClearExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardHistoryRepository;
import com.uhg.optum.financials.payments.payer.service.EncryptionAtRestService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardDetailsService;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.AmountConverter;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.lang3.ObjectUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;

import java.util.Map;
import java.util.List;
import java.util.Objects;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import java.util.function.Function;
import java.util.stream.Collectors;

import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.INFO;

@Slf4j
@Service
public class VirtualCardDetailsServiceImpl implements VirtualCardDetailsService {

	@Autowired
	private ZetaApiService zetaApiService;

	@Autowired
	private ModelConversionMapper cardDetailsMapper;

	@Autowired
	private CardCreationRepository cardCreationRepository;

	@Autowired
	private AuditLogRepository auditLogRepository;

	@Autowired
	private AuthExtractRepository authExtractRepository;

	@Autowired
	private ClearExtractRepository clearExtractRepository;

	@Autowired
	private LogUtility logUtility;

	@Autowired
	private ModelConversionMapper modelConversionMapper;

	@Autowired
	private EncryptionAtRestService encryptionAtRestService;

	@Autowired
	private VirtualCardHistoryRepository virtualCardHistoryRepository;

	private static final Gson JSON = new Gson();

	ObjectMapper mapper = new ObjectMapper();

	@Override
	public ResponseEntity<?> getVirtualCardDetails(UUID cardId) throws Exception {
		ResponseEntity<String> response = this.zetaApiService.getVirtualCard(cardId);
		if (response.getStatusCode().is2xxSuccessful()) {
			String body = response.getBody();
			if (body == null || body.isEmpty()) {
				throw new RuntimeException(
						"Failed to retrieve card details for cardId: " + cardId + ". Response body is null or empty");
			}
			CardDetailsDTO cardDetailsDto = JSON.fromJson(body, CardDetailsDTO.class);
			if (cardDetailsDto == null) {
				throw new NotFoundException("Card details not found for cardId: " + cardId);
			}
			return ResponseEntity.ok(cardDetailsDto);
		}
		else {
			CardErrorResponseDto errorResponseDto = JSON.fromJson(response.getBody(), CardErrorResponseDto.class);
			throw HttpExceptionUtil.buildException(response.getStatusCode(), getCardErrorMessage(errorResponseDto));
		}

	}

	@Override
	public CardCreationStatusDto getCardCreationStatus(String paymentId) throws Exception {
		CardCreationStatusDto cardCreationStatusDto = getRawCardCreationStatus(paymentId);
		cardCreationStatusDto.getData()
			.getInitialBalance()
			.setAmount(AmountConverter
				.toDollars(cardCreationStatusDto.getData().getInitialBalance().getAmount().longValue())
				.setScale(2));
		return cardCreationStatusDto;
	}

	@Override
	public CardCreationStatusDto getRawCardCreationStatus(String paymentId) throws Exception {
		Optional<CardCreation> cardCreation = this.cardCreationRepository
			.findTopByPaymentIdOrderByCreatedAtDesc(paymentId);
		CardCreationStatusDto cardCreationStatusDto;
		if (cardCreation.isEmpty()) {
			throw new NotFoundException(String.format(ApiConstants.CARD_CREATION_NOT_FOUND_IN_TABLE, paymentId));
		}
		else {
			UUID requestId = cardCreation.get().getId();
			ResponseEntity<String> response = this.zetaApiService.getCardCreationStatus(requestId);
			String body = response.getBody();
			if (body == null || body.isEmpty()) {
				throw new InternalServerException(ApiConstants.EMPTY_RESPONSE_FROM_CARD_API + paymentId);
			}

			if (!response.getStatusCode().is2xxSuccessful()) {
				throw new InternalServerException(response.getStatusCode().value() + " "
						+ HttpStatus.valueOf(response.getStatusCode().value()).getReasonPhrase());
			}

			cardCreationStatusDto = JSON.fromJson(body, CardCreationStatusDto.class);
		}
		if (cardCreationStatusDto == null) {
			throw new NotFoundException(ApiConstants.CARD_CREATION_STATUS_NOT_FOUND + paymentId);
		}

		if (cardCreationStatusDto.getData() == null || cardCreationStatusDto.getData().getInitialBalance() == null
				|| cardCreationStatusDto.getData().getInitialBalance().getAmount() == null) {
			throw new InternalServerException("Invalid card creation status response for paymentId: " + paymentId
					+ ". Missing data or initial balance");
		}
		return cardCreationStatusDto;

	}

	@Override
	public ResponseEntity<?> findCardAuditEvents(String paymentId) {
		this.logUtility.logMessage("Fetching card activity details", INFO, log);

		List<AuditLog> auditLogs = this.auditLogRepository.findByPaymentIdOrderByCreatedAtAsc(paymentId);

		if (auditLogs.isEmpty()) {
			throw new NotFoundException("No record found for card history");
		}

		Set<String> authReceiptIds = auditLogs.stream()
			.filter((auditLog) -> CardEventType.AUTH.toString().equalsIgnoreCase(auditLog.getEventType()))
			.map(AuditLog::getReceiptId)
			.filter(Objects::nonNull)
			.collect(Collectors.toSet());
		Map<String, AuthExtract> authByReceiptId = authReceiptIds.isEmpty() ? Map.of()
				: this.authExtractRepository.findByReceiptIdIn(authReceiptIds.stream().toList())
					.stream()
					.collect(
							Collectors.toMap(AuthExtract::getReceiptId, Function.identity(), (first, second) -> first));

		Set<String> clearReceiptIds = auditLogs.stream()
			.filter((auditLog) -> CardEventType.CLEARING.toString().equalsIgnoreCase(auditLog.getEventType()))
			.map(AuditLog::getReceiptId)
			.filter(ObjectUtils::isNotEmpty)
			.collect(Collectors.toSet());
		Map<String, ClearExtract> clearByReceiptId = clearReceiptIds.isEmpty() ? Map.of() : this.clearExtractRepository
			.findByReceiptIdIn(clearReceiptIds.stream().toList())
			.stream()
			.collect(Collectors.toMap(ClearExtract::getReceiptId, Function.identity(), (first, second) -> first));

		this.logUtility.logMessage("Mapping " + auditLogs.size() + " audit logs to dto", INFO, log);
		List<AuditLogDto> auditLogDtos = auditLogs.stream()
			.map((auditLog) -> mapAuditLogToDto(auditLog, authByReceiptId, clearByReceiptId))
			.collect(Collectors.toList());
		this.logUtility.logMessage(
				"Completed mapping audit logs for paymentId=" + paymentId + ", dtoCount=" + auditLogDtos.size(), INFO,
				log);

		return ResponseEntity.ok(auditLogDtos);
	}

	@Override
	public List<VirtualCardHistoryDto> findVirtualCardHistoryDetails(String cardGuid) {
		log.info("Finding virtual card history details");
		try {
			UUID cardUuid = UUID.fromString(cardGuid);
			List<VirtualCardHistory> virtualCardHistoryList = this.virtualCardHistoryRepository
				.findByCardGuidOrderByCreatedAtAsc(cardUuid);

			if (virtualCardHistoryList.isEmpty()) {
				throw new NotFoundException(ApiConstants.CARD_HISTORY_NOT_FOUND + cardGuid);
			}

			return virtualCardHistoryList.stream()
				.map((virtualCardHistory) -> this.modelConversionMapper.toVirtualCardHistoryDto(virtualCardHistory))
				.collect(Collectors.toList());
		}
		catch (IllegalArgumentException exception) {
			log.error("Invalid UUID format for cardGuid", exception);
			throw new BadRequestException(ApiConstants.INVALID_UUID_FORMAT + cardGuid);
		}
		catch (NotFoundException notFoundException) {
			log.error("Card history not found for cardGuid");
			throw notFoundException;
		}
		catch (Exception exception) {
			log.error("Error while fetching virtual card history details", exception);
			throw new NotFoundException(ApiConstants.ERROR_FETCHING_CARD_HISTORY + cardGuid);
		}
	}

	private AuditLogDto mapAuditLogToDto(AuditLog auditLog, Map<String, AuthExtract> authByReceiptId,
			Map<String, ClearExtract> clearByReceiptId) {

		AuditLogDto dto = this.modelConversionMapper.toAuditLogDto(auditLog);

		if (ObjectUtils.isEmpty(auditLog.getReceiptId())) {
			return dto;
		}

		String eventType = auditLog.getEventType();
		if (CardEventType.AUTH.toString().equalsIgnoreCase(eventType)) {

			handleAuthAmount(dto, authByReceiptId);
		}
		else if (CardEventType.CLEARING.toString().equalsIgnoreCase(eventType)) {

			handleClearingAmount(dto, clearByReceiptId);
		}
		else {
			this.logUtility.logMessage("Event type details processing", INFO, log);
		}

		return dto;
	}

	private void handleAuthAmount(AuditLogDto dto, Map<String, AuthExtract> authByReceiptId) {
		if (ObjectUtils.isEmpty(dto.getReceiptId())) {
			return;
		}
		this.logUtility.logMessage(
				"AUTH lookup from batch for receiptId=" + dto.getReceiptId() + " (auditLog id=" + dto.getId() + ")",
				INFO, log);
		AuthExtract authExtract = authByReceiptId.get(dto.getReceiptId());
		if (ObjectUtils.isNotEmpty(authExtract)) {
			dto.setAmount(authExtract.getAuthAmount());
		}

	}

	private void handleClearingAmount(AuditLogDto dto, Map<String, ClearExtract> clearByReceiptId) {
		if (ObjectUtils.isEmpty(dto.getReceiptId())) {
			return;
		}
		ClearExtract clearExtract = clearByReceiptId.get(dto.getReceiptId());
		if (ObjectUtils.isNotEmpty(clearExtract)) {

			dto.setAmount(clearExtract.getClearingAmount());
		}

	}

	private String getCardErrorMessage(CardErrorResponseDto errorResponseDto) {
		if (errorResponseDto == null || errorResponseDto.getMessage() == null
				|| errorResponseDto.getMessage().isBlank()) {
			return "Error occurred while fetching virtual card details.";
		}
		return errorResponseDto.getMessage();
	}

}
