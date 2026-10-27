package com.uhg.optum.financials.payments.payer.util;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.models.BlobProperties;
import com.azure.storage.blob.models.CopyStatusType;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.context.annotation.Lazy;
import org.springframework.stereotype.Component;

import java.io.IOException;

import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.INFO;

@Slf4j
@Lazy
@Component
public class StorageFileUtil {

	@Autowired
	@Qualifier("defaultBlobContainerClient")
	private BlobContainerClient blobContainerClient;

	@Autowired
	private SecretService secretService;

	@Autowired
	private LogUtility logUtility;

	public void deleteTheFile(BlobClient blobClient) {
		blobClient.delete();
		this.logUtility.logMessage("Blob Deleted :" + blobClient.getBlobName(), INFO, log);
	}

	public BlobClient getFileClient(String fileName) throws IOException {
		String blobName = fileName;
		if (blobName == null) {
			throw new IllegalArgumentException("File name cannot  be empty or null ");
		}
		try {
			BlobClient blobClient = this.blobContainerClient.getBlobClient(blobName);
			return blobClient;
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Exception in receive FileFromFileShare :" + ex.getMessage(), ERROR, log);
			throw new IOException("Unable to retrieve the blob client due to an internal error  ");
		}
	}

	public void moveFileToDirectory(String srcDir, String destDir) {
		BlobClient srcBlobClient = this.blobContainerClient.getBlobClient(srcDir);
		BlobClient destBlobClient = this.blobContainerClient.getBlobClient(destDir);
		destBlobClient.beginCopy(srcBlobClient.getBlobUrl(), null);
		BlobProperties properties = destBlobClient.getProperties();
		while (properties.getCopyStatus() == CopyStatusType.PENDING) {
			properties = destBlobClient.getProperties();
		}
		srcBlobClient.delete();
	}

	public void copyToBlob(BlobClient srcBlobClient, BlobClient destBlobClient) {
		destBlobClient.beginCopy(srcBlobClient.getBlobUrl(), null);
		BlobProperties properties = destBlobClient.getProperties();
		while (properties.getCopyStatus() == CopyStatusType.PENDING) {
			properties = destBlobClient.getProperties();
		}
	}

	public void copyFileToDirectory(String srcDir, String destDir) {
		BlobClient srcBlobClient = this.blobContainerClient.getBlobClient(srcDir);
		BlobClient destBlobClient = this.blobContainerClient.getBlobClient(destDir);
		destBlobClient.beginCopy(srcBlobClient.getBlobUrl(), null);
		BlobProperties properties = destBlobClient.getProperties();
		while (properties.getCopyStatus() == CopyStatusType.PENDING) {
			properties = destBlobClient.getProperties();
		}
	}

	public void deleteBlob(String srcDir) {
		BlobClient srcBlobClient = this.blobContainerClient.getBlobClient(srcDir);
		srcBlobClient.delete();
	}

}
