package com.uhg.optum.financials.payments.payer.util;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.models.BlobProperties;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

import java.io.IOException;

@ExtendWith(MockitoExtension.class)
class StorageFileUtilTests {

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private StorageFileUtil storageFileUtil;

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private SecretService secretService;

	@Mock
	private BlobClient blobClient;

	@Test
	void testDeleteTheFile() {
		this.storageFileUtil.deleteTheFile(this.blobClient);
		Assertions.assertThat(this.blobClient).isNotNull();
	}

	@Test
	void testGetFileClient() throws IOException {
		Mockito.when(this.blobContainerClient.getBlobClient(Mockito.anyString())).thenReturn(this.blobClient);
		Assertions.assertThat(this.storageFileUtil.getFileClient("test")).isNotNull();
	}

	@Test
	void testGetFileClientException() {
		Mockito.when(this.blobContainerClient.getBlobClient(Mockito.anyString())).thenThrow(new RuntimeException());
		Assertions.assertThatThrownBy(() -> this.storageFileUtil.getFileClient("test")).isInstanceOf(IOException.class);
	}

	@Test
	void testGetFileClientNull() {
		Assertions.assertThatThrownBy(() -> this.storageFileUtil.getFileClient(null))
			.isInstanceOf(IllegalArgumentException.class);
	}

	@Test
	void testMoveTheFile() {
		String srcDir = "sourceDir";
		String destDir = "destDir";
		BlobClient srcBlobClient = Mockito.mock(BlobClient.class);
		BlobClient destBlobClient = Mockito.mock(BlobClient.class);
		Mockito.when(this.blobContainerClient.getBlobClient(srcDir)).thenReturn(srcBlobClient);
		Mockito.when(this.blobContainerClient.getBlobClient(destDir)).thenReturn(destBlobClient);
		Mockito.when(destBlobClient.getProperties()).thenReturn(Mockito.mock(BlobProperties.class));

		this.storageFileUtil.moveFileToDirectory(srcDir, destDir);

		Mockito.verify(destBlobClient, Mockito.times(1)).beginCopy(srcBlobClient.getBlobUrl(), null);
		Mockito.verify(srcBlobClient, Mockito.times(1)).delete();
	}

	@Test
	void copyFileToProcessedDirectory_shouldCopyFile() {
		BlobClient srcBlobClient = Mockito.mock(BlobClient.class);
		BlobClient destBlobClient = Mockito.mock(BlobClient.class);
		Mockito.when(destBlobClient.getProperties()).thenReturn(Mockito.mock(BlobProperties.class));

		this.storageFileUtil.copyToBlob(srcBlobClient, destBlobClient);

		Mockito.verify(destBlobClient, Mockito.times(1)).beginCopy(srcBlobClient.getBlobUrl(), null);
	}

}
