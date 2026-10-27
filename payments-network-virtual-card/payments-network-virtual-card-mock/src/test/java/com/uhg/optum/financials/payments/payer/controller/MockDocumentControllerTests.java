package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.document.DocumentResponse;
import com.uhg.optum.financials.payments.payer.service.UploadService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;
import org.springframework.http.ResponseEntity;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;

import static org.assertj.core.api.BDDAssertions.then;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.BDDMockito.given;

class MockDocumentControllerTests {

	@Mock
	private UploadService azureContainerUploadService;

	@InjectMocks
	private MockDocumentController mockDocumentController;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void shouldUploadFileSuccessfully() throws Exception {
		MultipartFile file = Mockito.mock(MultipartFile.class);
		DocumentResponse documentResponse = Mockito.mock(DocumentResponse.class);
		given(this.azureContainerUploadService.uploadFile(file)).willReturn(documentResponse);

		ResponseEntity<DocumentResponse> response = this.mockDocumentController.uploadFile(file);

		then(response.getStatusCode().is2xxSuccessful()).isTrue();
		then(response.getBody()).isEqualTo(documentResponse);
	}

	@Test
	void shouldThrowIOExceptionWhenUploadFails() throws Exception {
		MultipartFile file = Mockito.mock(MultipartFile.class);
		given(this.azureContainerUploadService.uploadFile(file)).willThrow(new IOException("Upload failed"));

		assertThatThrownBy(() -> this.mockDocumentController.uploadFile(file)).isInstanceOf(IOException.class)
			.hasMessage("Upload failed");
	}

}
