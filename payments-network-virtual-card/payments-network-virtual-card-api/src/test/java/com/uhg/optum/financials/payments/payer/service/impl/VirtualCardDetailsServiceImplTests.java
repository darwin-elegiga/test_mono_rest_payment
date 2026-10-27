package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.AuditLogDto;
import com.uhg.optum.financials.payments.payer.dto.CardCreationStatusDto;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.AuditLog;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.ClearExtract;
import com.uhg.optum.financials.payments.payer.repository.AuditLogRepository;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.ClearExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardHistoryRepository;
import com.uhg.optum.financials.payments.payer.service.ZetaApiService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.ResponseEntity;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.Collections;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verifyNoInteractions;

@ExtendWith(MockitoExtension.class)
class VirtualCardDetailsServiceImplTests {

	@Mock
	private CardCreationRepository cardCreationRepository;

	@Mock
	private ZetaApiService zetaApiService;

	@InjectMocks
	private VirtualCardDetailsServiceImpl service;

	@Mock
	private AuditLogRepository auditLogRepository;

	@Mock
	private AuthExtractRepository authExtractRepository;

	@Mock
	private ClearExtractRepository clearExtractRepository;

	@Mock
	private LogUtility logUtility;

	@Mock(name = "modelConversionMapper")
	private ModelConversionMapper modelConversionMapper;

	@Mock(name = "cardDetailsMapper")
	private ModelConversionMapper cardDetailsMapper;

	@Mock
	private VirtualCardHistoryRepository virtualCardHistoryRepository;

	@Test
	void getCardCreationStatus_success() throws Exception {
		String paymentId = "test-payment-id";
		UUID requestId = UUID.randomUUID();
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.willReturn(Optional.of(cardCreation));
		String json = "{\"status\":\"SUCCESS\",\"data\":{\"cardGuid\":\"146786ee-a546-439d-9c00-2db09bfb354d\","
				+ "\"initialBalance\":{\"currency\":\"USD\",\"amount\":20},\"spendRules\":[]}}";
		given(this.zetaApiService.getCardCreationStatus(requestId)).willReturn(ResponseEntity.ok(json));

		CardCreationStatusDto result = this.service.getCardCreationStatus(paymentId);
		assertThat(result.getStatus()).isEqualTo("SUCCESS");
		assertThat(result.getData().getCardGuid()).isEqualTo("146786ee-a546-439d-9c00-2db09bfb354d");
		assertThat(result.getData().getInitialBalance().getAmount()).isEqualByComparingTo(new BigDecimal("0.20"));
	}

	@Test
	void getRawCardCreationStatus_success_preservesZetaAmount() throws Exception {
		String paymentId = "test-payment-id";
		UUID requestId = UUID.randomUUID();
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.willReturn(Optional.of(cardCreation));
		String json = "{\"status\":\"SUCCESS\",\"data\":{\"cardGuid\":\"146786ee-a546-439d-9c00-2db09bfb354d\","
				+ "\"initialBalance\":{\"currency\":\"USD\",\"amount\":20},\"spendRules\":[]}}";
		given(this.zetaApiService.getCardCreationStatus(requestId)).willReturn(ResponseEntity.ok(json));

		CardCreationStatusDto result = this.service.getRawCardCreationStatus(paymentId);

		assertThat(result.getStatus()).isEqualTo("SUCCESS");
		assertThat(result.getData().getCardGuid()).isEqualTo("146786ee-a546-439d-9c00-2db09bfb354d");
		assertThat(result.getData().getInitialBalance().getAmount()).isEqualByComparingTo(new BigDecimal("20"));
	}

	@Test
	void getCardCreationStatus_notFound() {
		String paymentId = "not-found";
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.willReturn(Optional.empty());
		assertThatThrownBy(() -> this.service.getCardCreationStatus(paymentId)).isInstanceOf(NotFoundException.class)
			.hasMessageContaining("Card creation details not found");
	}

	@Test
	void getCardCreationStatus_emptyBody() throws Exception {
		String paymentId = "test-payment-id";
		UUID requestId = UUID.randomUUID();
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.willReturn(Optional.of(cardCreation));
		given(this.zetaApiService.getCardCreationStatus(requestId)).willReturn(ResponseEntity.ok(""));
		assertThatThrownBy(() -> this.service.getCardCreationStatus(paymentId))
			.isInstanceOf(InternalServerException.class)
			.hasMessageContaining("Empty response body");
	}

	@Test
	void getCardCreationStatus_nullBody() throws Exception {
		String paymentId = "test-payment-id";
		UUID requestId = UUID.randomUUID();
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.willReturn(Optional.of(cardCreation));
		given(this.zetaApiService.getCardCreationStatus(requestId)).willReturn(ResponseEntity.ok(null));
		assertThatThrownBy(() -> this.service.getCardCreationStatus(paymentId))
			.isInstanceOf(InternalServerException.class)
			.hasMessageContaining("Empty response body");
	}

	@Test
	void getCardCreationStatus_nullDeserializedDto() throws Exception {
		String paymentId = "test-payment-id";
		UUID requestId = UUID.randomUUID();
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.willReturn(Optional.of(cardCreation));
		// Simulate Gson returning null for deserialization
		given(this.zetaApiService.getCardCreationStatus(requestId)).willReturn(ResponseEntity.ok("null"));
		assertThatThrownBy(() -> this.service.getCardCreationStatus(paymentId)).isInstanceOf(NotFoundException.class)
			.hasMessageContaining("Card Creation Status not found");
	}

	@Test
	void getCardCreationStatus_missingData_throwsRuntimeException() throws Exception {
		String paymentId = "test-payment-id";
		UUID requestId = UUID.randomUUID();
		CardCreation cardCreation = new CardCreation();
		cardCreation.setId(requestId);
		given(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.willReturn(Optional.of(cardCreation));
		given(this.zetaApiService.getCardCreationStatus(requestId))
			.willReturn(ResponseEntity.ok("{\"status\":\"SUCCESS\"}"));

		assertThatThrownBy(() -> this.service.getCardCreationStatus(paymentId))
			.isInstanceOf(InternalServerException.class)
			.hasMessageContaining("Missing data or initial balance");
	}

	@Test
	void findCardAuditEvents_NoLogs_ReturnsNotFound() {
		String paymentId = "pay-empty";
		given(this.auditLogRepository.findByPaymentIdOrderByCreatedAtAsc(paymentId))
			.willReturn(Collections.emptyList());

		Assertions.assertThatThrownBy(() -> this.service.findCardAuditEvents(paymentId))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("No record found for card history");
		then(this.auditLogRepository).should(times(1)).findByPaymentIdOrderByCreatedAtAsc(paymentId);
		verifyNoInteractions(this.authExtractRepository, this.clearExtractRepository);
	}

	@Test
	void findCardAuditEvents_WithAuthAndClearingEvents_SetsAmountsForMultipleReceiptIds() {
		String paymentId = "pay-multi";

		AuditLog authLog1 = new AuditLog();
		authLog1.setPaymentId(paymentId);
		authLog1.setEventType("AUTH");
		authLog1.setReceiptId("R123");
		authLog1.setCreatedAt(LocalDateTime.now().minusSeconds(3));

		AuditLog authLog2 = new AuditLog();
		authLog2.setPaymentId(paymentId);
		authLog2.setEventType("AUTH");
		authLog2.setReceiptId("R124");
		authLog2.setCreatedAt(LocalDateTime.now().minusSeconds(2));

		AuditLog clearLog1 = new AuditLog();
		clearLog1.setPaymentId(paymentId);
		clearLog1.setEventType("CLEARING");
		clearLog1.setReceiptId("RCLEAR1");
		clearLog1.setCreatedAt(LocalDateTime.now().minusSeconds(1));

		AuditLog clearLog2 = new AuditLog();
		clearLog2.setPaymentId(paymentId);
		clearLog2.setEventType("CLEARING");
		clearLog2.setReceiptId("RCLEAR2");
		clearLog2.setCreatedAt(LocalDateTime.now());

		given(this.auditLogRepository.findByPaymentIdOrderByCreatedAtAsc(paymentId))
			.willReturn(List.of(authLog1, authLog2, clearLog1, clearLog2));

		AuditLogDto authDto1 = new AuditLogDto();
		authDto1.setReceiptId("R123");
		authDto1.setEventType("AUTH");
		given(this.modelConversionMapper.toAuditLogDto(authLog1)).willReturn(authDto1);

		AuditLogDto authDto2 = new AuditLogDto();
		authDto2.setReceiptId("R124");
		authDto2.setEventType("AUTH");
		given(this.modelConversionMapper.toAuditLogDto(authLog2)).willReturn(authDto2);

		AuditLogDto clearDto1 = new AuditLogDto();
		clearDto1.setReceiptId("RCLEAR1");
		clearDto1.setEventType("CLEARING");
		given(this.modelConversionMapper.toAuditLogDto(clearLog1)).willReturn(clearDto1);

		AuditLogDto clearDto2 = new AuditLogDto();
		clearDto2.setReceiptId("RCLEAR2");
		clearDto2.setEventType("CLEARING");
		given(this.modelConversionMapper.toAuditLogDto(clearLog2)).willReturn(clearDto2);

		AuthExtract authExtract1 = new AuthExtract();
		authExtract1.setReceiptId("R123");
		authExtract1.setAuthAmount(new BigDecimal("123.45"));
		AuthExtract authExtract2 = new AuthExtract();
		authExtract2.setReceiptId("R124");
		authExtract2.setAuthAmount(new BigDecimal("67.89"));
		given(this.authExtractRepository.findByReceiptIdIn(anyList())).willReturn(List.of(authExtract1, authExtract2));

		ClearExtract clearExtract1 = new ClearExtract();
		clearExtract1.setReceiptId("RCLEAR1");
		clearExtract1.setClearingAmount(new BigDecimal("50.00"));
		ClearExtract clearExtract2 = new ClearExtract();
		clearExtract2.setReceiptId("RCLEAR2");
		clearExtract2.setClearingAmount(new BigDecimal("75.25"));
		given(this.clearExtractRepository.findByReceiptIdIn(anyList()))
			.willReturn(List.of(clearExtract1, clearExtract2));

		ResponseEntity<?> response = this.service.findCardAuditEvents(paymentId);

		assertThat(response.getStatusCode().value()).isEqualTo(200);
		@SuppressWarnings("unchecked")
		List<AuditLogDto> body = (List<AuditLogDto>) response.getBody();
		assertThat(body).hasSize(4);
		assertThat(body.get(0).getAmount()).isEqualByComparingTo(new BigDecimal("123.45"));
		assertThat(body.get(1).getAmount()).isEqualByComparingTo(new BigDecimal("67.89"));
		assertThat(body.get(2).getAmount()).isEqualByComparingTo(new BigDecimal("50.00"));
		assertThat(body.get(3).getAmount()).isEqualByComparingTo(new BigDecimal("75.25"));

		then(this.authExtractRepository).should(times(1)).findByReceiptIdIn(anyList());
		then(this.clearExtractRepository).should(times(1)).findByReceiptIdIn(anyList());
		then(this.authExtractRepository).should(never()).findByReceiptId(anyString());
		then(this.clearExtractRepository).should(never()).findByReceiptId(anyString());
	}

	@Test
	void findCardAuditEvents_WithClearingEvent_SetsAmountFromClearExtract() {
		String paymentId = "pay-clear";
		AuditLog auditLog = new AuditLog();
		auditLog.setPaymentId(paymentId);
		auditLog.setEventType("CLEARING");
		auditLog.setReceiptId("RCLEAR");
		auditLog.setCreatedAt(LocalDateTime.now());
		given(this.auditLogRepository.findByPaymentIdOrderByCreatedAtAsc(paymentId)).willReturn(List.of(auditLog));

		AuditLogDto dto = new AuditLogDto();
		dto.setReceiptId("RCLEAR");
		dto.setEventType("CLEARING");
		given(this.modelConversionMapper.toAuditLogDto(auditLog)).willReturn(dto);

		ClearExtract clearExtract = new ClearExtract();
		clearExtract.setReceiptId("RCLEAR");
		clearExtract.setClearingAmount(new BigDecimal("50.00"));
		given(this.clearExtractRepository.findByReceiptIdIn(List.of("RCLEAR"))).willReturn(List.of(clearExtract));

		ResponseEntity<?> response = this.service.findCardAuditEvents(paymentId);

		assertThat(response.getStatusCode().value()).isEqualTo(200);
		@SuppressWarnings("unchecked")
		List<AuditLogDto> body = (List<AuditLogDto>) response.getBody();
		assertThat(body).isNotNull();
		assertThat(body).hasSize(1);
		assertThat(body.get(0)).isNotNull();
		assertThat(body.get(0).getAmount()).isEqualByComparingTo(new BigDecimal("50.00"));

		then(this.clearExtractRepository).should(times(1)).findByReceiptIdIn(List.of("RCLEAR"));
		then(this.authExtractRepository).should(never()).findByReceiptId(anyString());
		then(this.authExtractRepository).should(never()).findByReceiptIdIn(anyList());
	}

	@Test
	void findCardAuditEvents_WithOtherEvent_NoRepoCalls() {
		String paymentId = "pay-other";
		AuditLog auditLog = new AuditLog();
		auditLog.setPaymentId(paymentId);
		auditLog.setEventType("CARD_CREATED");
		auditLog.setReceiptId("ROTHER");
		auditLog.setCreatedAt(LocalDateTime.now());
		given(this.auditLogRepository.findByPaymentIdOrderByCreatedAtAsc(paymentId)).willReturn(List.of(auditLog));

		// Mock the conversion for other event types
		AuditLogDto dto = new AuditLogDto();
		dto.setReceiptId("ROTHER");
		dto.setEventType("CARD_CREATED");
		given(this.modelConversionMapper.toAuditLogDto(auditLog)).willReturn(dto);

		ResponseEntity<?> response = this.service.findCardAuditEvents(paymentId);

		assertThat(response.getStatusCode().value()).isEqualTo(200);
		assertThat(response.getBody()).isNotNull();
		List<?> body = (List<?>) response.getBody();
		assertThat(body).hasSize(1);
		assertThat(body.get(0)).isNotNull();
		assertThat(body.get(0)).isInstanceOf(AuditLogDto.class);

		verifyNoInteractions(this.authExtractRepository, this.clearExtractRepository);
	}

	@Test
	void findCardAuditEvents_EmptyReceiptId_DoesNotCallRepos() {
		String paymentId = "pay-empty-receipt";
		AuditLog auditLog = new AuditLog();
		auditLog.setPaymentId(paymentId);
		auditLog.setEventType("AUTH");
		auditLog.setReceiptId(null);
		auditLog.setCreatedAt(LocalDateTime.now());
		given(this.auditLogRepository.findByPaymentIdOrderByCreatedAtAsc(paymentId)).willReturn(List.of(auditLog));

		AuditLogDto dto = new AuditLogDto();
		dto.setReceiptId(null);
		dto.setEventType("AUTH");
		given(this.modelConversionMapper.toAuditLogDto(auditLog)).willReturn(dto);

		ResponseEntity<?> response = this.service.findCardAuditEvents(paymentId);

		assertThat(response.getStatusCode().value()).isEqualTo(200);
		List<?> body = (List<?>) response.getBody();
		assertThat(body).hasSize(1);
		assertThat(body.get(0)).isInstanceOf(AuditLogDto.class);
		AuditLogDto resultDto = (AuditLogDto) body.get(0);
		assertThat(resultDto.getAmount()).isNull();

		verifyNoInteractions(this.authExtractRepository, this.clearExtractRepository);
	}

	@Test
	void findVirtualCardHistoryDetails_invalidUUID_throwsBadRequestException() {
		String invalidGuid = "not-a-uuid";
		Assertions.assertThatThrownBy(() -> this.service.findVirtualCardHistoryDetails(invalidGuid))
			.isInstanceOf(com.uhg.optum.financials.payments.payer.exception.BadRequestException.class)
			.hasMessageContaining("Invalid UUID format");
	}

}
