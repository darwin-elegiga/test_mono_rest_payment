package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.FileProcessingStatusDto;
import com.uhg.optum.financials.payments.payer.service.ExtractFileService;
import io.swagger.v3.oas.annotations.Operation;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping("/v1/file-processing-status")
public class ExtractFileController {

	@Autowired
	private ExtractFileService extractFileService;

	@Operation(summary = "Get all the extract files processed on a specific date",
			description = " **NOTE:** Retrieves detailed information about all extract files processed at a particular date.")
	@GetMapping("/files")
	@PreAuthorize("hasAuthority('org:networks:read')")
	public ResponseEntity<List<FileProcessingStatusDto>> getFileProcessingStatusByDate(
			@RequestParam("date") String date) {
		List<FileProcessingStatusDto> result = this.extractFileService.getFileProcessingStatusByDate(date);
		return ResponseEntity.ok(result);
	}

}
