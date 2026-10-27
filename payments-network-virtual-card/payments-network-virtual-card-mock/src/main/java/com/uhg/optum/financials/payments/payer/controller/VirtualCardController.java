package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.CardCreation;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Optional;

@RestController
@Slf4j
@RequestMapping("v1/card")
public class VirtualCardController {

	private final CardCreationRepository cardCreationRepository;

	public VirtualCardController(CardCreationRepository cardCreationRepository) {
		this.cardCreationRepository = cardCreationRepository;
	}

	@GetMapping("/creation-details/{requestId}")
	public ResponseEntity<?> getCardCreationDetails(@PathVariable String requestId) {
		log.info("Fetching card creation details for requestId: {}", requestId);
		Optional<CardCreation> cardCreation = this.cardCreationRepository
			.findTopByPaymentIdOrderByCreatedAtDesc(requestId);
		if (cardCreation.isPresent()) {
			return new ResponseEntity<>(cardCreation, HttpStatus.OK);
		}
		throw new NotFoundException("No Record found for card creation details");
	}

}
