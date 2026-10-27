package com.uhg.optum.financials.payments.payer.controller;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.springframework.http.HttpHeaders;
import org.springframework.mock.web.MockHttpServletResponse;
import org.springframework.web.servlet.mvc.method.annotation.StreamingResponseBody;
import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;

import com.azure.core.util.BinaryData;
import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.service.impl.FileServiceImpl;

import java.io.ByteArrayOutputStream;
import java.io.IOException;

class FileServiceImplTests {

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private BlobClient blobClient;

	@Mock
	private BinaryData binaryData;

	@InjectMocks
	private FileServiceImpl fileService;

	@BeforeEach
	void setUp() {
		try (AutoCloseable closeable = MockitoAnnotations.openMocks(this)) {
			// Initialization logic
		}
		catch (Exception ex) {
			throw new RuntimeException(ex);
		}
	}

	@Test
	void testDownloadBlobFile() throws IOException {
		// Arrange
		String fileNameCloudPath = "path/to/file.txt";
		String expectedContent = "";
		MockHttpServletResponse response = new MockHttpServletResponse();

		given(this.blobContainerClient.getBlobClient(fileNameCloudPath)).willReturn(this.blobClient);
		given(this.blobClient.downloadContent()).willReturn(this.binaryData);
		given(this.binaryData.toBytes()).willReturn(expectedContent.getBytes());

		// Act
		StreamingResponseBody responseBody = this.fileService.downloadBlobFile(fileNameCloudPath, response);
		ByteArrayOutputStream outputStream = new ByteArrayOutputStream();
		responseBody.writeTo(outputStream);
		String actualContent = outputStream.toString();

		// Assert
		assertThat(response.getHeader(HttpHeaders.CONTENT_DISPOSITION)).isEqualTo("attachment; filename=file.txt");
		assertThat(actualContent).isEqualTo(expectedContent);
		then(this.blobContainerClient).should().getBlobClient(fileNameCloudPath);
		then(this.blobClient).should().downloadContent();
	}

}
