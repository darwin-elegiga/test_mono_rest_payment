package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.constant.ApiConstants;
import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.payments.payer.dto.CardCreationStatusDto;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.dto.VirtualCardHistoryDto;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.CardCancellation;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.CardReset;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuditLogRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCancellationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardResetRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.CancelCardService;
import com.uhg.optum.financials.payments.payer.service.ResetCardService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardDetailsService;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.math.BigDecimal;
import java.util.Collections;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;

@ExtendWith(MockitoExtension.class)
public class VirtualCardControllerTests {

	@InjectMocks
	VirtualCardController virtualCardController;

	@Mock
	VirtualCardDetailsService virtualCardDetailsService;

	@Mock
	CancelCardService cancelCardService;

	@Mock
	LogUtility logUtility;

	@Mock
	VirtualCardsRepository virtualCardsRepository;

	@Mock
	CardCancellationRepository cardCancellationRepository;

	@Mock
	AuditLogger auditLogger;

	@Mock
	ResetCardService resetCardService;

	@Mock
	AuditLogRepository auditLogRepository;

	@Mock
	CardCreationRepository cardCreationRepository;

	@Mock
	CardResetRepository cardResetRepository;

	@Test
	void testGetVirtualCardDetails() throws Exception {
		UUID virtualCardId = UUID.fromString("ad4f51d6-56ca-41e3-a531-3652c64a495c");
		this.virtualCardController.getVirtualCardDetails(virtualCardId);
		Mockito.verify(this.virtualCardDetailsService, Mockito.times(1)).getVirtualCardDetails(virtualCardId);
	}

	@Test
	void cancelCard() {
		CancelCardDto cancelCardDto = new CancelCardDto();
		Assertions.assertThat(this.virtualCardController.cancelCard(cancelCardDto).getStatusCode().is2xxSuccessful())
			.isTrue();
	}

	@Test
	void getCardCancellatinDetails() {
		String requestID = "ad4f51d6-56ca-41e3-a531-3652c64a495c";
		BDDMockito.when(this.cardCancellationRepository.findByPaymentId(any())).thenReturn(new CardCancellation());
		Assertions
			.assertThat(
					this.virtualCardController.getCardCancellationDetails(requestID).getStatusCode().is2xxSuccessful())
			.isTrue();
	}

	@Test
	void getVirtualCardDetail() {
		String requestID = "12345";
		BDDMockito.when(this.virtualCardsRepository.findByPaymentId(any())).thenReturn(new VirtualCard());
		Assertions.assertThat(this.virtualCardController.getCardDetails(requestID).getStatusCode().is2xxSuccessful())
			.isTrue();
	}

	@Test
	void getCardCancellatinDetailsNotFound() {
		String requestID = "12345";
		BDDMockito.when(this.cardCancellationRepository.findByPaymentId(any())).thenReturn(null);
		Assertions.assertThatThrownBy(() -> this.virtualCardController.getCardCancellationDetails(requestID))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("No Record found for the paymentId " + requestID);
	}

	@Test
	void getVirtualCardDetailNotFound() {
		String requestID = "12345";
		BDDMockito.when(this.virtualCardsRepository.findByPaymentId(any())).thenReturn(null);
		Assertions.assertThatThrownBy(() -> this.virtualCardController.getCardDetails(requestID))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("Virtual card not created yet for the paymentId " + requestID);
	}

	@Test
	void resetCard() {
		ResetCardRequest resetCardRequest = new ResetCardRequest();
		Assertions.assertThat(this.virtualCardController.resetCard(resetCardRequest).getStatusCode().is2xxSuccessful())
			.isTrue();
	}

	@Test
	void testGetCreateCardStatus() throws Exception {
		String paymentId = "test-payment-id";

		CardCreationStatusDto.CardData cardData = new CardCreationStatusDto.CardData();
		cardData.setCardGuid("146786ee-a546-439d-9c00-2db09bfb354d");
		CardCreationStatusDto.InitialBalance initialBalance = new CardCreationStatusDto.InitialBalance();
		initialBalance.setCurrency("USD");
		initialBalance.setAmount(new BigDecimal(20));
		cardData.setInitialBalance(initialBalance);
		cardData.setSpendRules(java.util.Collections.emptyList());

		CardCreationStatusDto statusDto = new CardCreationStatusDto();
		statusDto.setClientRequestId("d775a99e-4f32-48c4-8acb-afa371f43df0e");
		statusDto.setStatus("SUCCESS");
		statusDto.setCreatedAt("2025-08-20T08:36:09.000534Z");
		statusDto.setCompletedAt("2025-08-20T08:36:13.068495Z");
		statusDto.setData(cardData);
		statusDto.setError(null);

		Mockito.when(this.virtualCardDetailsService.getCardCreationStatus(paymentId)).thenReturn(statusDto);

		ResponseEntity<CardCreationStatusDto> response = this.virtualCardController.getCreateCardStatus(paymentId);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(statusDto);
		Assertions.assertThat(response.getBody().getStatus()).isEqualTo("SUCCESS");
		Assertions.assertThat(response.getBody().getData().getCardGuid())
			.isEqualTo("146786ee-a546-439d-9c00-2db09bfb354d");
		Assertions.assertThat(response.getBody().getData().getInitialBalance().getAmount())
			.isEqualTo(new BigDecimal(20));
		Assertions.assertThat(response.getBody().getData().getInitialBalance().getCurrency()).isEqualTo("USD");

		Mockito.verify(this.logUtility)
			.logMessage("Fetching card creation details for paymentId:" + paymentId, ApiConstants.INFO,
					LoggerFactory.getLogger(VirtualCardController.class));
		Mockito.verify(this.virtualCardDetailsService).getCardCreationStatus(paymentId);
	}

	@Test
	@SuppressWarnings("unchecked")
	void testGetVirtualCardAuditDetails() {
		String paymentId = "test-payment-id-123";
		ResponseEntity<?> mockResponse = ResponseEntity.ok("Audit details found");

		BDDMockito.when(this.virtualCardDetailsService.findCardAuditEvents(paymentId))
			.thenReturn((ResponseEntity) mockResponse);

		ResponseEntity<?> response = this.virtualCardController.getVirtualCardAuditDetails(paymentId);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo("Audit details found");
		Mockito.verify(this.virtualCardDetailsService, Mockito.times(1)).findCardAuditEvents(paymentId);
	}

	@Test
	@SuppressWarnings("unchecked")
	void testGetVirtualCardAuditDetailsNotFound() {
		String paymentId = "non-existent-payment-id";
		BDDMockito.when(this.virtualCardDetailsService.findCardAuditEvents(paymentId))
			.thenThrow(new NotFoundException("No record found for card history"));

		Assertions.assertThatThrownBy(() -> this.virtualCardController.getVirtualCardAuditDetails(paymentId))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("No record found for card history");
		Mockito.verify(this.virtualCardDetailsService, Mockito.times(1)).findCardAuditEvents(paymentId);
	}

	@Test
	void testGetCardCreationDetailsPresent() {
		String paymentId = "test-payment-id";
		CardCreation cardCreation = new CardCreation();
		BDDMockito.when(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.thenReturn(Optional.of(cardCreation));

		ResponseEntity<?> response = this.virtualCardController.getCardCreationDetails(paymentId);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(cardCreation);

		Mockito.verify(this.logUtility)
			.logMessage("Fetching card creation details for paymentId:" + paymentId, ApiConstants.INFO,
					LoggerFactory.getLogger(VirtualCardController.class));
		Mockito.verify(this.cardCreationRepository).findTopByPaymentIdOrderByCreatedAtDesc(paymentId);
	}

	@Test
	void testGetCardCreationDetailsNotPresent() {
		String paymentId = "test-payment-id";
		BDDMockito.when(this.cardCreationRepository.findTopByPaymentIdOrderByCreatedAtDesc(paymentId))
			.thenReturn(Optional.empty());

		Assertions.assertThatThrownBy(() -> this.virtualCardController.getCardCreationDetails(paymentId))
			.isInstanceOf(NotFoundException.class)
			.hasMessage(ApiConstants.CARD_CREATION_NOT_FOUND + paymentId);

		Mockito.verify(this.logUtility)
			.logMessage("Fetching card creation details for paymentId:" + paymentId, ApiConstants.INFO,
					LoggerFactory.getLogger(VirtualCardController.class));
		Mockito.verify(this.cardCreationRepository).findTopByPaymentIdOrderByCreatedAtDesc(paymentId);
	}

	@Test
	void testGetCardResetDetailsPresent() {
		String receiptId = "receipt-123";
		CardReset cardReset = new CardReset();
		cardReset.setReceiptId(receiptId);
		BDDMockito.when(this.cardResetRepository.findByReceiptId(receiptId)).thenReturn(cardReset);

		ResponseEntity<?> response = this.virtualCardController.getCardResetDetails(receiptId);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(cardReset);
		Mockito.verify(this.cardResetRepository).findByReceiptId(receiptId);
	}

	@Test
	void testGetCardResetDetailsNotFound() {
		String receiptId = "missing-receipt";
		BDDMockito.when(this.cardResetRepository.findByReceiptId(receiptId)).thenReturn(null);

		Assertions.assertThatThrownBy(() -> this.virtualCardController.getCardResetDetails(receiptId))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("No card reset record found for receiptId " + receiptId);
		Mockito.verify(this.cardResetRepository).findByReceiptId(receiptId);
	}

	@Test
	void testGetVirtualCardHistoryDetailsNonEmpty() {
		String cardGuid = "test-card-guid";
		VirtualCardHistoryDto historyDto = new VirtualCardHistoryDto();
		List<VirtualCardHistoryDto> historyList = Collections.singletonList(historyDto);
		BDDMockito.when(this.virtualCardDetailsService.findVirtualCardHistoryDetails(cardGuid)).thenReturn(historyList);

		ResponseEntity<?> response = this.virtualCardController.getVirtualCardHistoryDetails(cardGuid);

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isEqualTo(historyList);

		Mockito.verify(this.virtualCardDetailsService).findVirtualCardHistoryDetails(cardGuid);
	}

	@Test
	void testGetVirtualCardHistoryDetailsEmpty() {
		String cardGuid = "8d90bff7-daf2-4929-94ba-fde6b65f94ab";
		BDDMockito.when(this.virtualCardDetailsService.findVirtualCardHistoryDetails(cardGuid))
			.thenThrow(new NotFoundException(ApiConstants.CARD_HISTORY_NOT_FOUND + cardGuid));

		Assertions.assertThatThrownBy(() -> this.virtualCardController.getVirtualCardHistoryDetails(cardGuid))
			.isInstanceOf(NotFoundException.class)
			.hasMessage(ApiConstants.CARD_HISTORY_NOT_FOUND + cardGuid);

		Mockito.verify(this.virtualCardDetailsService).findVirtualCardHistoryDetails(cardGuid);
	}

	@Test
	void testGetVirtualCardHistoryDetailsInvalidUUID() {
		String invalidCardGuid = "invalid-uuid-format";
		BDDMockito.when(this.virtualCardDetailsService.findVirtualCardHistoryDetails(invalidCardGuid))
			.thenThrow(new BadRequestException(ApiConstants.INVALID_UUID_FORMAT + invalidCardGuid));

		Assertions.assertThatThrownBy(() -> this.virtualCardController.getVirtualCardHistoryDetails(invalidCardGuid))
			.isInstanceOf(BadRequestException.class)
			.hasMessage(ApiConstants.INVALID_UUID_FORMAT + invalidCardGuid);

		Mockito.verify(this.virtualCardDetailsService).findVirtualCardHistoryDetails(invalidCardGuid);
	}

}
