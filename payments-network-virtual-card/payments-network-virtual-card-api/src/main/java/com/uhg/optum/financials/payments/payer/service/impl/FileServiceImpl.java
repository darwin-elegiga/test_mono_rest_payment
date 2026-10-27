package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.core.util.BinaryData;
import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.service.FileService;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.http.HttpHeaders;
import org.springframework.stereotype.Service;
import org.springframework.web.servlet.mvc.method.annotation.StreamingResponseBody;
import static com.uhg.optum.financials.payments.payer.constant.FileConstants.ATTACHMENT_FILENAME;

@Service
public class FileServiceImpl implements FileService {

	private final BlobContainerClient blobContainerClient;

	public FileServiceImpl(@Qualifier("defaultBlobContainerClient") BlobContainerClient blobContainerClient) {
		this.blobContainerClient = blobContainerClient;
	}

	@Override
	public StreamingResponseBody downloadBlobFile(String fileNameCloudPath, HttpServletResponse response) {

		BinaryData binaryData = this.blobContainerClient.getBlobClient(fileNameCloudPath).downloadContent();
		response.setHeader(HttpHeaders.CONTENT_DISPOSITION,
				ATTACHMENT_FILENAME + fileNameCloudPath.substring(fileNameCloudPath.lastIndexOf("/") + 1));
		return binaryData::writeTo;
	}

}
