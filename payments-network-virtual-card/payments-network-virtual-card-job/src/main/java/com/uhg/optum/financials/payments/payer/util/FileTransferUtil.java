package com.uhg.optum.financials.payments.payer.util;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.shared.core.secrets.SecretService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.io.BufferedInputStream;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileWriter;
import java.io.IOException;

@Slf4j
@Component
public class FileTransferUtil {

	@Autowired
	@Qualifier("defaultBlobContainerClient")
	private BlobContainerClient blobContainerClient;

	@Autowired
	private SecretService secretService;

	@Value("${local.blobContainerName}")
	private String blobContainerName;

	public BlobClient receiveFileFromFileShare(String fileName) throws IOException {
		if (fileName == null) {
			throw new IllegalArgumentException("File name cannot be empty or null ");
		}
		try {
			BlobClient blobClient = this.blobContainerClient.getBlobClient(fileName);
			return blobClient;
		}
		catch (Exception ex) {
			log.error("Exception in receive FileFromFileShare :{}", ex.getMessage());
			throw new IOException("Unable to retrieve the blob client due to an internal error");
		}
	}

	public void uploadFile(File file) throws IOException {
		try (BufferedInputStream bufferedInputStream = new BufferedInputStream(new FileInputStream(file));) {
			BlobClient blobClient = this.blobContainerClient.getBlobClient(file.getName());
			blobClient.upload(bufferedInputStream, bufferedInputStream.available(), true);
		}
		catch (Exception ex) { // catch Exception to wrap any error
			throw new IOException("Unable to retrieve the blob client due to an internal error", ex);
		}
	}

	public void uploadFile(BlobContainerClient blobContainerClient, File file) throws IOException {
		uploadFile(blobContainerClient, file, file.getPath());
	}

	public void uploadFile(BlobContainerClient blobContainerClient, File file, String destinationPath)
			throws IOException {
		try (BufferedInputStream bufferedInputStream = new BufferedInputStream(new FileInputStream(file));) {
			BlobClient blobClient = blobContainerClient.getBlobClient(destinationPath);
			blobClient.upload(bufferedInputStream, bufferedInputStream.available(), true);
		}
		catch (IOException ex) {
			throw new IOException("Unable to retrieve the blob client due to an internal error");
		}
	}

	public void uploadFile(BlobContainerClient blobContainerClient, byte[] content, String destinationPath)
			throws IOException {
		try (BufferedInputStream bufferedInputStream = new BufferedInputStream(
				new java.io.ByteArrayInputStream(content))) {
			BlobClient blobClient = blobContainerClient.getBlobClient(destinationPath);
			blobClient.upload(bufferedInputStream, bufferedInputStream.available(), true);
		}
		catch (IOException ex) {
			throw new IOException("Unable to retrieve the blob client due to an internal error");
		}
	}

	public boolean writeToFile(FileWriter myWriter, String trailer) throws IOException {
		try (BufferedWriter writer = new BufferedWriter(myWriter)) {
			writer.write(trailer);
		}
		return true;
	}

	public void deleteTheFile(BlobClient blobClient) {
		blobClient.delete();
		log.info("Blob Deleted :{}", blobClient.getBlobName());
	}

}
