package com.uhg.optum.financials.payments.payer.util;

import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.sas.BlobSasPermission;
import com.azure.storage.blob.sas.BlobServiceSasSignatureValues;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.context.annotation.DependsOn;
import org.springframework.context.annotation.Lazy;
import org.springframework.stereotype.Component;

import java.net.URI;
import java.net.URISyntaxException;
import java.time.Duration;
import java.time.OffsetDateTime;

@Slf4j
@DependsOn("blobStorageConfig")
@Lazy
@Component
public final class SasUrlUtil {

	public final BlobContainerClient blobContainerClient;

	public SasUrlUtil(@Qualifier("defaultBlobContainerClient") BlobContainerClient blobContainerClient) {
		this.blobContainerClient = blobContainerClient;
	}

	public String generateSasUrl(String blobName, String timeToLive) throws BadRequestException {
		OffsetDateTime expiryTime = OffsetDateTime.now().plus(Duration.ofMinutes(Long.parseLong(timeToLive)));
		BlobSasPermission permission = new BlobSasPermission().setReadPermission(true);

		BlobServiceSasSignatureValues values = new BlobServiceSasSignatureValues(expiryTime, permission)
			.setStartTime(OffsetDateTime.now())
			.setContentType("application/octet-stream");

		if (!this.blobContainerClient.getBlobClient(blobName).exists()) {
			throw new BadRequestException("Blob does not exist: " + blobName);
		}
		String sasToken = this.blobContainerClient.getBlobClient(blobName).generateSas(values);
		return this.blobContainerClient.getBlobClient(blobName).getBlobUrl() + "?" + sasToken;
	}

	public String refreshSasUrl(String existingSasUrl, String timeToLive) throws BadRequestException {
		try {
			URI uri = new URI(existingSasUrl);
			String containerName = this.blobContainerClient.getBlobContainerName();
			String blobName = uri.getPath()
				.substring(uri.getPath().indexOf(containerName) + containerName.length() + 1);
			if (!this.blobContainerClient.getBlobClient(blobName).exists()) {
				throw new BadRequestException("Blob does not exist: " + blobName);
			}
			return generateSasUrl(blobName, timeToLive);
		}
		catch (URISyntaxException ex) {
			throw new BadRequestException("Invalid SAS URL: " + ex.getMessage());
		}
		catch (BadRequestException ex) {
			throw ex;
		}
		catch (Exception ex) {
			throw new BadRequestException("Failed to refresh SAS URL: " + ex.getMessage());
		}
	}

}
