package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.CancelCardDto;
import com.uhg.optum.financials.payments.payer.dto.CardCreationStatusDto;
import com.uhg.optum.financials.payments.payer.dto.ResetCardRequest;
import com.uhg.optum.financials.payments.payer.dto.VirtualCardHistoryDto;
import com.uhg.optum.financials.payments.payer.model.CardCancellation;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.model.CardReset;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.repository.CardCancellationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardResetRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.CancelCardService;
import com.uhg.optum.financials.payments.payer.service.ResetCardService;
import com.uhg.optum.financials.payments.payer.service.VirtualCardDetailsService;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.util.Utility.sanitizeForLog;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.INFO;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.CARD_CREATION_NOT_FOUND;
import static com.uhg.optum.financials.payments.payer.constant.ApiConstants.SUCCESS;

@Slf4j
@Tag(name = "Virtual Card Management",
		description = "APIs for managing virtual cards, including creation, cancellation, and retrieval of card details.")
@RestController
@RequestMapping("v1")
public class VirtualCardController {

	@Autowired
	private VirtualCardDetailsService virtualCardDetailsService;

	@Autowired
	CancelCardService cancelCardService;

	@Autowired
	CardCancellationRepository cardCancellationRepository;

	@Autowired
	VirtualCardsRepository virtualCardsRepository;

	@Autowired
	CardCreationRepository cardCreationRepository;

	@Autowired
	CardResetRepository cardResetRepository;

	@Autowired
	LogUtility logUtility;

	@Autowired
	AuditLogger auditLogger;

	@Autowired
	ResetCardService resetCardService;

	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/{cardId}")
	public ResponseEntity<?> getVirtualCardDetails(@PathVariable UUID cardId) throws Exception {
		log.info("Fetching virtual card details");
		return this.virtualCardDetailsService.getVirtualCardDetails(cardId);
	}

	@Operation(summary = "Cancel Card",
			description = " **NOTE**: Accepts a cancel request and sends it to the Storage Queue for processing. This endpoint is used to cancel the card.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping("/cancel-card")
	public ResponseEntity<String> cancelCard(@Valid @RequestBody CancelCardDto cancelCardDto) {
		this.logUtility.logMessage("cancel card request received : " + cancelCardDto, INFO, log);
		this.auditLogger.logCancelReceived(cancelCardDto.getPaymentId());
		this.cancelCardService.sendCancelCardRequest(cancelCardDto);
		return new ResponseEntity<>(SUCCESS, HttpStatus.OK);
	}

	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/cancel/{paymentId}")
	public ResponseEntity<?> getCardCancellationDetails(@PathVariable String paymentId) {
		String sanitizedPaymentId = sanitizeForLog(paymentId);
		CardCancellation cardCancellation = this.cardCancellationRepository.findByPaymentId(paymentId);
		if (cardCancellation != null) {
			return new ResponseEntity<>(cardCancellation, HttpStatus.OK);
		}
		else {
			log.error("No card cancellation record found for paymentId: {}", sanitizedPaymentId);
			throw new NotFoundException("No Record found for the paymentId " + paymentId);
		}
	}

	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/virtual-card/{paymentID}")
	public ResponseEntity<?> getCardDetails(@PathVariable String paymentID) {
		String sanitizedPaymentId = sanitizeForLog(paymentID);
		VirtualCard virtualCard = this.virtualCardsRepository.findByPaymentId(paymentID);

		if (virtualCard == null) {
			log.error("No virtual card found for paymentId: {}", sanitizedPaymentId);
			throw new NotFoundException("Virtual card not created yet for the paymentId " + paymentID);
		}
		else {
			return new ResponseEntity<>(virtualCard, HttpStatus.OK);
		}
	}

	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/creation-details/{paymentId}")
	public ResponseEntity<?> getCardCreationDetails(@PathVariable String paymentId) {
		String sanitizedPaymentId = sanitizeForLog(paymentId);
		this.logUtility.logMessage("Fetching card creation details for paymentId:" + sanitizedPaymentId, INFO, log);
		Optional<CardCreation> cardCreation = this.cardCreationRepository
			.findTopByPaymentIdOrderByCreatedAtDesc(paymentId);
		if (cardCreation.isPresent()) {
			return new ResponseEntity<>(cardCreation.get(), HttpStatus.OK);
		}
		else {
			log.error("No card creation details found for paymentId: {}", sanitizedPaymentId);
			throw new NotFoundException(CARD_CREATION_NOT_FOUND + paymentId);
		}
	}

	@Operation(summary = "Get Card Reset Details",
			description = "Endpoint to retrieve card reset details using the receipt ID.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/card-reset/{receiptId}")
	public ResponseEntity<?> getCardResetDetails(@PathVariable String receiptId) {
		String sanitizedReceiptId = sanitizeForLog(receiptId);
		CardReset cardReset = this.cardResetRepository.findByReceiptId(receiptId);
		if (cardReset != null) {
			return new ResponseEntity<>(cardReset, HttpStatus.OK);
		}
		else {
			log.error("No card reset record found for receiptId: {}", sanitizedReceiptId);
			throw new NotFoundException("No card reset record found for receiptId " + receiptId);
		}
	}

	@Operation(summary = "Reset Card",
			description = " **NOTE**: Accepts a reset request and sends it to the Storage Queue for processing. This endpoint is used to reset the card.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping("/reset-card")
	public ResponseEntity<String> resetCard(@Valid @RequestBody ResetCardRequest resetCardRequest) {
		this.logUtility.logMessage("reset card request received : " + resetCardRequest, INFO, log);
		this.resetCardService.sendResetCardRequest(resetCardRequest);
		return new ResponseEntity<>(SUCCESS, HttpStatus.OK);
	}

	@Operation(summary = "Create Card Status Polling",
			description = " **NOTE**: Accepts a paymentId and returns the status of the card creation process."
					+ " This endpoint is used to poll the status of the card creation.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/card-polling/{paymentId}")
	public ResponseEntity<CardCreationStatusDto> getCreateCardStatus(@PathVariable String paymentId) throws Exception {
		this.logUtility.logMessage("Fetching card creation details for paymentId:" + paymentId, INFO, log);
		CardCreationStatusDto cardCreationStatusDto = this.virtualCardDetailsService.getCardCreationStatus(paymentId);
		return ResponseEntity.ok(cardCreationStatusDto);
	}

	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/card-audit-details/{paymentId}")
	public ResponseEntity<?> getVirtualCardAuditDetails(@PathVariable String paymentId) {
		return this.virtualCardDetailsService.findCardAuditEvents(paymentId);
	}

	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/card-history/{cardGuid}")
	public ResponseEntity<List<VirtualCardHistoryDto>> getVirtualCardHistoryDetails(@PathVariable String cardGuid) {
		List<VirtualCardHistoryDto> virtualCardHistoryDetails = this.virtualCardDetailsService
			.findVirtualCardHistoryDetails(cardGuid);
		return ResponseEntity.ok(virtualCardHistoryDetails);
	}

}
