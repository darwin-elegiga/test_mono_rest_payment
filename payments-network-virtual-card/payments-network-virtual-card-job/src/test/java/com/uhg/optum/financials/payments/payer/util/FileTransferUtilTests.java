package com.uhg.optum.financials.payments.payer.util;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentMatchers;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoSettings;
import org.mockito.quality.Strictness;
import org.mockito.junit.jupiter.MockitoExtension;
import java.io.BufferedInputStream;
import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.BDDMockito.willDoNothing;
import static org.mockito.Mockito.times;

@ExtendWith(MockitoExtension.class)
@MockitoSettings(strictness = Strictness.LENIENT)
class FileTransferUtilTests {

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private BlobClient blobClient;

	@InjectMocks
	private FileTransferUtil fileTransferUtil;

	@BeforeEach
	void setUp() {
		// No-op, handled by @InjectMocks
	}

	@AfterEach
	void cleanUpTestFile() {
		File file = new File("test.txt");
		if (file.exists()) {
			file.delete();
		}
	}

	@Test
	@DisplayName("givenValidFileName_whenReceiveFileFromFileShare_thenReturnsBlobClient")
	void givenValidFileName_whenReceiveFileFromFileShare_thenReturnsBlobClient() throws IOException {
		// given
		String fileName = "test.txt";
		given(this.blobContainerClient.getBlobClient(fileName)).willReturn(this.blobClient);
		// when
		BlobClient result = this.fileTransferUtil.receiveFileFromFileShare(fileName);
		// then
		assertThat(result).isEqualTo(this.blobClient);
	}

	@Test
	@DisplayName("givenNullFileName_whenReceiveFileFromFileShare_thenThrowsIllegalArgumentException")
	void givenNullFileName_whenReceiveFileFromFileShare_thenThrowsIllegalArgumentException() {
		// when/then
		assertThatThrownBy(() -> this.fileTransferUtil.receiveFileFromFileShare(null))
			.isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("File name cannot be empty or null");
	}

	@Test
	@DisplayName("givenException_whenReceiveFileFromFileShare_thenThrowsIOException")
	void givenException_whenReceiveFileFromFileShare_thenThrowsIOException() {
		// given
		String fileName = "test.txt";
		given(this.blobContainerClient.getBlobClient(fileName)).willThrow(new RuntimeException("fail"));
		// when/then
		assertThatThrownBy(() -> this.fileTransferUtil.receiveFileFromFileShare(fileName))
			.isInstanceOf(IOException.class)
			.hasMessageContaining("Unable to retrieve the blob client due to an internal error");
	}

	@Test
	@DisplayName("givenValidFile_whenUploadFile_thenUploadsSuccessfully")
	void givenValidFile_whenUploadFile_thenUploadsSuccessfully() throws Exception {
		// given
		File file = new File("test.txt");
		if (!file.exists()) {
			try (FileWriter writer = new FileWriter(file)) {
				writer.write("dummy");
			}
		}
		given(this.blobContainerClient.getBlobClient(file.getName())).willReturn(this.blobClient);
		willDoNothing().given(this.blobClient)
			.upload(ArgumentMatchers.any(BufferedInputStream.class), ArgumentMatchers.anyLong(),
					ArgumentMatchers.eq(true));
		// when
		this.fileTransferUtil.uploadFile(file);
		// then
		then(this.blobClient).should(times(1))
			.upload(ArgumentMatchers.any(BufferedInputStream.class), ArgumentMatchers.anyLong(),
					ArgumentMatchers.eq(true));
	}

	@Test
	@DisplayName("givenIOException_whenUploadFile_thenThrowsIOException")
	void givenIOException_whenUploadFile_thenThrowsIOException() {
		// given
		File file = new File("test.txt");
		given(this.blobContainerClient.getBlobClient(file.getName())).willReturn(this.blobClient);
		// Use lenient stubbing and clear it after the test to avoid
		// UnnecessaryStubbingException
		org.mockito.stubbing.Stubber stubber = org.mockito.Mockito.lenient().doThrow(new RuntimeException("fail"));
		stubber.when(this.blobClient)
			.upload(ArgumentMatchers.any(BufferedInputStream.class), ArgumentMatchers.anyLong(),
					ArgumentMatchers.eq(true));
		try {
			// when/then
			assertThatThrownBy(() -> this.fileTransferUtil.uploadFile(file)).isInstanceOf(IOException.class)
				.hasMessageContaining("Unable to retrieve the blob client due to an internal error");
		}
		finally {
			org.mockito.Mockito.reset(this.blobClient);
		}
	}

	@Test
	@DisplayName("givenValidWriter_whenWriteToFile_thenReturnsTrue")
	void givenValidWriter_whenWriteToFile_thenReturnsTrue() throws IOException {
		// given
		boolean result;
		try (FileWriter writer = new FileWriter("test.txt")) {
			// when
			result = this.fileTransferUtil.writeToFile(writer, "trailer");
		}
		// then
		assertThat(result).isTrue();
	}

	@Test
	@DisplayName("givenBlobClient_whenDeleteTheFile_thenDeletesBlob")
	void givenBlobClient_whenDeleteTheFile_thenDeletesBlob() {
		// when
		this.fileTransferUtil.deleteTheFile(this.blobClient);
		// then
		then(this.blobClient).should(times(1)).delete();
	}

}
