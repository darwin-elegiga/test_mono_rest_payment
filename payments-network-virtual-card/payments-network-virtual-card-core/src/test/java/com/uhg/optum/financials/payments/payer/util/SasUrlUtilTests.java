package com.uhg.optum.financials.payments.payer.util;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
public class SasUrlUtilTests {

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private BlobClient blobClient;

	@InjectMocks
	private SasUrlUtil sasUrlUtil;

	@Test
	void generateSasUrl_validBlobName_returnsSasUrl() {
		String blobName = "valid_blob.txt";
		String ttl = "15";
		String expectedSasUrl = "https://example.com/valid_blob.txt?sasToken";

		Mockito.when(this.blobContainerClient.getBlobClient(blobName)).thenReturn(this.blobClient);
		Mockito.when(this.blobClient.exists()).thenReturn(true);
		Mockito.when(this.blobClient.generateSas(Mockito.any())).thenReturn("sasToken");
		Mockito.when(this.blobClient.getBlobUrl()).thenReturn("https://example.com/valid_blob.txt");

		String sasUrl = this.sasUrlUtil.generateSasUrl(blobName, ttl);

		Assertions.assertThat(expectedSasUrl).isEqualTo(sasUrl);
	}

	@Test
	void generateSasUrl_blobDoesNotExist_throwsBadRequestException() {
		String blobName = "nonexistent_blob.txt";
		String ttl = "15";

		Mockito.when(this.blobContainerClient.getBlobClient(blobName)).thenReturn(this.blobClient);
		Mockito.when(this.blobClient.exists()).thenReturn(false);

		Assertions.assertThatException().isThrownBy(() -> this.sasUrlUtil.generateSasUrl(blobName, ttl));
	}

	@Test
	void refreshSasUrl_invalidSasUrl_throwsBadRequestException() {
		String existingSasUrl = "invalid_sas_url";
		String ttl = "15";

		Assertions.assertThatException().isThrownBy(() -> this.sasUrlUtil.refreshSasUrl(existingSasUrl, ttl));

	}

}
