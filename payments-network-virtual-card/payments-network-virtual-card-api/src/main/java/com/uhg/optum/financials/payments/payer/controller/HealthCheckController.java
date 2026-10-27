
package com.uhg.optum.financials.payments.payer.controller;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.health.actuate.endpoint.HealthEndpoint;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@Tag(name = "Health Check")
@RestController
@RequestMapping("/v1/virtual-card")
public class HealthCheckController {

	private final HealthEndpoint healthEndpoint;

	@Autowired
	public HealthCheckController(HealthEndpoint healthEndpoint) {
		this.healthEndpoint = healthEndpoint;
	}

	@Operation(summary = "Checks to see if the application can connect to all required services")
	@GetMapping("/health-check")
	@PreAuthorize("hasAuthority('org:networks:read')")
	public Object healthCheck() {
		return this.healthEndpoint.health();
	}

}
