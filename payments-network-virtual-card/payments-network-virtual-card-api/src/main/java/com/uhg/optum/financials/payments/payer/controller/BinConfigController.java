package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.BinResponse;
import com.uhg.optum.financials.payments.payer.dto.CreateBinRequest;
import com.uhg.optum.financials.payments.payer.dto.UpdateBinRequest;
import com.uhg.optum.financials.payments.payer.service.BinConfigService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping(path = "/v1/bins", produces = "application/json")
@RequiredArgsConstructor
@Slf4j
@Tag(name = "Bin Management APIs",
		description = "APIs for configuring and managing bins associated with virtual cards.")
public class BinConfigController {

	private final BinConfigService binService;

	@Operation(summary = "Bin Management API - Get All Bins",
			description = " **NOTE**: This endpoint is used to get all bins.")
	@PreAuthorize("hasAuthority('org:networks:read')")
	@GetMapping
	public List<BinResponse> getAllBins() {
		return this.binService.getAllBins();
	}

	@Operation(summary = "Bin Management API - Add Bin",
			description = " **NOTE**: This endpoint is used to add new bin in the system.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PostMapping(consumes = "application/json")
	@ResponseStatus(HttpStatus.CREATED)
	public BinResponse saveBin(@Valid @RequestBody CreateBinRequest request) {
		return this.binService.saveBin(request);
	}

	@Operation(summary = "Bin Management API - Update Bin",
			description = " **NOTE**: This endpoint is used to update bin info in the system.")
	@PreAuthorize("hasAuthority('org:networks:write')")
	@PutMapping(path = "/{id}", consumes = "application/json")
	public BinResponse updateBin(@PathVariable Long id, @Valid @RequestBody UpdateBinRequest request) {
		return this.binService.updateBin(id, request);
	}

}
