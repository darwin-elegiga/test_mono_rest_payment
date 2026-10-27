package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationRegisteredEvent;
import com.uhg.optum.financials.payments.payer.dto.organization.OrganizationUpdatedDto;
import com.uhg.optum.financials.payments.payer.service.OrganizationPublisherService;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import static com.uhg.optum.financials.payments.payer.util.Utility.sanitizeIdForLog;

@RestController
@Slf4j
@RequestMapping("v1/organizations")
@Tag(name = "Organizations", description = "Mocking the Organizations events")
public class OrganizationController {

	@Autowired
	OrganizationPublisherService organizationPublisherService;

	@PostMapping("/register")
	public ResponseEntity<String> produceOrgRegistered(
			@RequestBody OrganizationRegisteredEvent organizationRegisteredEvent) {
		log.info("Sending org registered message to event bus");
		return ResponseEntity
			.ok(this.organizationPublisherService.publishOrganizationRegisteredEvent(organizationRegisteredEvent));
	}

	@PostMapping("/update")
	public ResponseEntity<String> produceOrgUpdated(@RequestBody OrganizationUpdatedDto organizationUpdatedDto) {
		log.info("Sending org updated message to event bus");
		return ResponseEntity
			.ok(this.organizationPublisherService.publishOrganizationUpdatedEvent(organizationUpdatedDto));
	}

	@GetMapping("/{orgId}")
	public ResponseEntity<?> getOrganization(@PathVariable String orgId) {
		log.info("Fetching org registration details for orgId: {}", sanitizeIdForLog(orgId));
		return ResponseEntity.ok(this.organizationPublisherService.getOrgRegDetails(orgId));
	}

}
