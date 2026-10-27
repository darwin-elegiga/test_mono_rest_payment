package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.dto.document.DocumentResponse;
import com.uhg.optum.financials.shared.core.storage.AzureStorageUtil;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockedStatic;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;

import static org.assertj.core.api.BDDAssertions.then;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.willThrow;

@ExtendWith(MockitoExtension.class)
class AzureContainerUploadServiceImplTests {

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private MultipartFile multipartFile;

	@InjectMocks
	private AzureContainerUploadServiceImpl azureContainerUploadServiceImpl;

	@BeforeEach
	void setUp() {

	}

	@Test
	void uploadFile_success() throws IOException {
		String fileName = "testfile.txt";
		byte[] fileBytes = new byte[] { 1, 2, 3 };
		given(this.multipartFile.getOriginalFilename()).willReturn(fileName);
		given(this.multipartFile.getBytes()).willReturn(fileBytes);

		try (MockedStatic<AzureStorageUtil> utilities = org.mockito.Mockito.mockStatic(AzureStorageUtil.class)) {
			utilities.when(
					() -> AzureStorageUtil.createBlobItem(this.blobContainerClient, "document/" + fileName, fileBytes))
				.thenReturn(true); // Must return boolean, not null

			DocumentResponse response = this.azureContainerUploadServiceImpl.uploadFile(this.multipartFile);

			then(response.getDocumentType()).isEqualTo("vcp");
			then(response.getDocumentUrl()).isEqualTo("document/" + fileName);
			then(response.getMetadata()).isNotNull();
		}
	}

	@Test
	void uploadFile_invalidFileName_throwsException() {
		given(this.multipartFile.getOriginalFilename()).willReturn("..//badfile.txt");
		assertThatThrownBy(() -> this.azureContainerUploadServiceImpl.uploadFile(this.multipartFile))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid filename");
	}

	@Test
	void uploadFile_ioException_propagates() throws IOException {
		String fileName = "testfile.txt";
		given(this.multipartFile.getOriginalFilename()).willReturn(fileName);
		willThrow(new IOException("IO error")).given(this.multipartFile).getBytes();
		assertThatThrownBy(() -> this.azureContainerUploadServiceImpl.uploadFile(this.multipartFile))
			.isInstanceOf(IOException.class)
			.hasMessageContaining("IO error");
	}

}
