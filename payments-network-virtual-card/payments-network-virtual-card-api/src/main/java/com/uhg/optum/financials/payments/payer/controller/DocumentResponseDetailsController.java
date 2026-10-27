package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.model.DocumentResponseDetails;
import com.uhg.optum.financials.payments.payer.service.DocumentResponseDetailsService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@Tag(name = "Document Response Details")
@RequestMapping("v1/documents")
public class DocumentResponseDetailsController {

	@Autowired
	private DocumentResponseDetailsService documentResponseDetailsService;

	@Operation(summary = "Get Document Response Details",
			description = "Endpoint to retrieve the document response details by its Network Payment ID.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping("/{networkPaymentId}")
	public ResponseEntity<List<DocumentResponseDetails>> getDocumentResponseDetails(
			@PathVariable String networkPaymentId) {
		return ResponseEntity.ok(this.documentResponseDetailsService.getDocumentResponseDetails(networkPaymentId));
	}

}
