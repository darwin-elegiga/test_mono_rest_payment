package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.document.DocumentResponse;
import com.uhg.optum.financials.payments.payer.service.UploadService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;

@RestController
@Slf4j
@RequestMapping("v1/documents")
public class MockDocumentController {

	@Autowired
	private UploadService azureContainerUploadService;

	@PostMapping(consumes = { MediaType.MULTIPART_FORM_DATA_VALUE }, produces = { MediaType.APPLICATION_JSON_VALUE })
	public ResponseEntity<DocumentResponse> uploadFile(@RequestParam("file") MultipartFile file) throws IOException {

		DocumentResponse documentResponse = this.azureContainerUploadService.uploadFile(file);

		return ResponseEntity.ok(documentResponse);
	}

}
