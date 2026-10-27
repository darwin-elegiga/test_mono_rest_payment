package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.dto.document.DocumentResponse;
import com.uhg.optum.financials.payments.payer.service.UploadService;
import com.uhg.optum.financials.shared.core.storage.AzureStorageUtil;
import lombok.extern.slf4j.Slf4j;
import net.minidev.json.JSONObject;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;

@Service
@Slf4j
public class AzureContainerUploadServiceImpl implements UploadService {

	@Autowired
	@Qualifier("defaultBlobContainerClient")
	private BlobContainerClient blobContainerClient;

	@Override
	public DocumentResponse uploadFile(MultipartFile file) throws IOException {
		String fileName = file.getOriginalFilename();

		if (fileName.contains("..") || fileName.contains("/") || fileName.contains("\\")) {
			throw new IllegalArgumentException("Invalid filename");
		}
		String fileNameWithPath = "document" + "/" + fileName;

		log.info("Uploading file to blob container");

		AzureStorageUtil.createBlobItem(this.blobContainerClient, fileNameWithPath, file.getBytes());
		DocumentResponse response = new DocumentResponse();
		response.setDocumentType("vcp");
		response.setDocumentUrl(fileNameWithPath);
		response.setMetadata(new JSONObject());
		return response;

	}

}
