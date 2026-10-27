package com.uhg.optum.financials.payments.payer.util;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.models.AccessTier;

public final class CloudStorageUtil {

	private CloudStorageUtil() {
	}

	public static void moveBlob(BlobContainerClient blobContainerClient, String srcFilePath, String dstFilePath,
			boolean isArchive) {
		BlobClient srcBlobClient = blobContainerClient.getBlobClient(srcFilePath);
		BlobClient dstBlobClient = blobContainerClient.getBlobClient(dstFilePath);
		dstBlobClient.copyFromUrl(srcBlobClient.getBlobUrl());
		if (isArchive) {
			dstBlobClient.setAccessTier(AccessTier.ARCHIVE);
		}
		srcBlobClient.delete();
	}

}
