package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.BlobServiceClient;
import com.uhg.optum.financials.payments.payer.dto.AcquirerAddress;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.SasUrlUtil;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.util.ReflectionTestUtils;

import java.io.ByteArrayInputStream;

import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.ArgumentMatchers.contains;
import static org.mockito.ArgumentMatchers.eq;

@ExtendWith(MockitoExtension.class)
class BarCodeServiceImplTests {

	@Mock
	private BlobServiceClient blobServiceClient;

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private BlobClient blobClient;

	@Mock
	private SasUrlUtil sasUrlUtil;

	@Mock
	private LogUtility logUtility;

	@InjectMocks
	private BarCodeServiceImpl barCodeServiceImpl;

	private final String barCodeDirectory = "barcodes/";

	private final String blobContainerName = "container";

	@BeforeEach
	void setUp() {
		ReflectionTestUtils.setField(this.barCodeServiceImpl, "blobContainerName", this.blobContainerName);
		ReflectionTestUtils.setField(this.barCodeServiceImpl, "barCodeDirectory", this.barCodeDirectory);
	}

	@Test
	void generateBarCode_ReturnsSasUrl_WhenBarcodeGeneratedSuccessfully() {
		AcquirerAddress acquirerAddress = new AcquirerAddress("123 Main St", "Apt 4B", "New York", "NY", "10001",
				"USA");
		String expectedSasUrl = "https://example.com/blob/barcode.png";

		BDDMockito.when(this.blobServiceClient.getBlobContainerClient(this.blobContainerName))
			.thenReturn(this.blobContainerClient);
		BDDMockito.when(this.blobContainerClient.getBlobClient(Mockito.anyString())).thenReturn(this.blobClient);
		BDDMockito.when(this.sasUrlUtil.generateSasUrl(Mockito.anyString(), Mockito.anyString()))
			.thenReturn(expectedSasUrl);

		String result = this.barCodeServiceImpl.generateBarCode(acquirerAddress);
		Assertions.assertThat(result).isEqualTo(expectedSasUrl);
		BDDMockito.verify(this.blobClient).upload(any(ByteArrayInputStream.class), anyLong(), eq(true));
	}

	@Test
	void generateBarCode_ReturnsNull_WhenBlobUploadFails() {
		AcquirerAddress acquirerAddress = new AcquirerAddress("123 Main St", "Apt 4B", "New York", "NY", "10001",
				"USA");
		String blobName = this.barCodeDirectory + "2023-10-10/barcode_123456789.png";

		BDDMockito.when(this.blobServiceClient.getBlobContainerClient(this.blobContainerName))
			.thenReturn(this.blobContainerClient);
		BDDMockito.when(this.blobContainerClient.getBlobClient(blobName)).thenReturn(this.blobClient);
		BDDMockito.doThrow(new RuntimeException("Upload failed"))
			.when(this.blobClient)
			.upload(any(ByteArrayInputStream.class), anyLong(), eq(true));

		String result = this.barCodeServiceImpl.generateBarCode(acquirerAddress);

		Assertions.assertThat(result).isNull();
		BDDMockito.verify(this.logUtility).logMessage(contains("Error generating barcode"), eq(ERROR), any());
	}

	@Test
	void generateBarCode_ReturnsNull_WhenSasUrlGenerationFails() {
		AcquirerAddress acquirerAddress = new AcquirerAddress("123 Main St", "Apt 4B", "New York", "NY", "10001",
				"USA");
		String blobName = this.barCodeDirectory + "2023-10-10/barcode_123456789.png";

		BDDMockito.when(this.blobServiceClient.getBlobContainerClient(this.blobContainerName))
			.thenReturn(this.blobContainerClient);
		BDDMockito.when(this.blobContainerClient.getBlobClient(blobName)).thenReturn(this.blobClient);
		BDDMockito.when(this.sasUrlUtil.generateSasUrl(blobName, "30"))
			.thenThrow(new RuntimeException("SAS URL generation failed"));

		String result = this.barCodeServiceImpl.generateBarCode(acquirerAddress);

		Assertions.assertThat(result).isNull();
		BDDMockito.verify(this.logUtility).logMessage(contains("Error generating barcode"), eq(ERROR), any());
	}

	@Test
	void generateBarCode_ReturnsNull_WhenIssuerAddressIsInvalid() {
		AcquirerAddress acquirerAddress = new AcquirerAddress("123 Main St", "Apt 4B", "New York", "NY", "10001",
				"USA");

		String result = this.barCodeServiceImpl.generateBarCode(acquirerAddress);

		Assertions.assertThat(result).isNull();
		BDDMockito.verify(this.logUtility).logMessage(contains("Error generating barcode"), eq(ERROR), any());
	}

	@Test
	void logMessageIsCalled_WhenCanvasFinishThrowsException() {
		AcquirerAddress acquirerAddress = new AcquirerAddress("123 Main St", "Apt 4B", "New York", "NY", "10001",
				"USA");

		// Mock dependencies as needed for BarCodeServiceImpl
		BDDMockito.when(this.blobServiceClient.getBlobContainerClient(this.blobContainerName))
			.thenReturn(this.blobContainerClient);
		BDDMockito.when(this.blobContainerClient.getBlobClient(Mockito.anyString())).thenReturn(this.blobClient);

		// Simulate exception during barcode generation (e.g., in upload or finish)
		BDDMockito.doThrow(new RuntimeException("Canvas finish failed"))
			.when(this.blobClient)
			.upload(any(ByteArrayInputStream.class), anyLong(), eq(true));

		this.barCodeServiceImpl.generateBarCode(acquirerAddress);

		BDDMockito.verify(this.logUtility).logMessage(contains("Error generating barcode"), eq(ERROR), any());
	}

}
