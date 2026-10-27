package com.uhg.optum.financials.payments.payer.util;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class CloudStorageUtilTests {

	@InjectMocks
	private CloudStorageUtil cloudStorageUtil;

	@Mock
	BlobClient blobClient;

	@Mock
	BlobContainerClient blobContainerClient;

	@Test
	void testMoveBlob() {

		Mockito.when(this.blobContainerClient.getBlobClient(Mockito.anyString())).thenReturn(this.blobClient);
		Mockito.when(this.blobClient.getBlobUrl()).thenReturn("testURL");
		this.cloudStorageUtil.moveBlob(this.blobContainerClient, "srcFilePath", "dstFilePath", true);

	}

}
