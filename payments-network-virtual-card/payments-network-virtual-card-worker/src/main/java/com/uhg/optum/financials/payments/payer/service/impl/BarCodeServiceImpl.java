package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.BlobServiceClient;
import com.uhg.optum.financials.payments.payer.dto.AcquirerAddress;
import com.uhg.optum.financials.payments.payer.service.BarCodeService;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.SasUrlUtil;
import lombok.extern.slf4j.Slf4j;
import org.krysalis.barcode4j.HumanReadablePlacement;
import org.krysalis.barcode4j.impl.code128.Code128Bean;
import org.krysalis.barcode4j.output.bitmap.BitmapCanvasProvider;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.awt.image.BufferedImage;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;

import static com.uhg.optum.financials.payments.payer.constant.Constants.BARCODE_IMAGE_START_NAME;
import static com.uhg.optum.financials.payments.payer.constant.Constants.IMAGE_TYPE;
import static com.uhg.optum.financials.payments.payer.constant.Constants.IMAGE_TYPE_FORMAT;
import static com.uhg.optum.financials.payments.payer.constant.Constants.SPACE;
import static com.uhg.optum.financials.payments.payer.constant.ValidationConstants.ERROR;
import static com.uhg.optum.financials.shared.client.util.DateTimeUtil.toNewYorkZoneYYYYMMDD;

@Slf4j
@Service
public class BarCodeServiceImpl implements BarCodeService {

	@Value("${storage.containerName}")
	private String blobContainerName;

	@Value("${document.template.bar-code}")
	private String barCodeDirectory;

	private final BlobServiceClient blobServiceClient;

	private final SasUrlUtil sasUrlUtil;

	private final LogUtility logUtility;

	private static final String STORAGE_URL_TIME_TO_LIVE = "30"; // 30 minutes

	public BarCodeServiceImpl(BlobServiceClient blobServiceClient, SasUrlUtil sasUrlUtil, LogUtility logUtility) {
		this.blobServiceClient = blobServiceClient;
		this.sasUrlUtil = sasUrlUtil;
		this.logUtility = logUtility;
	}

	@Override
	public String generateBarCode(AcquirerAddress acquirerAddress) {
		String data = acquirerAddress.getCity() + SPACE + acquirerAddress.getState() + SPACE
				+ acquirerAddress.getCountry() + SPACE + acquirerAddress.getZipCode();
		// Use Code 128 for full ASCII support
		Code128Bean bean = new Code128Bean();
		bean.setMsgPosition(HumanReadablePlacement.HRP_NONE);
		bean.setBarHeight(20.2f);
		bean.setModuleWidth(0.8f);

		BitmapCanvasProvider canvas = null;
		try (ByteArrayOutputStream baos = new ByteArrayOutputStream()) {
			canvas = new BitmapCanvasProvider(300, BufferedImage.TYPE_BYTE_BINARY, false, 0);
			bean.generateBarcode(canvas, data);

			// Ensure finish is always called
			canvas.finish();

			BufferedImage barcodeImage = canvas.getBufferedImage();
			javax.imageio.ImageIO.write(barcodeImage, IMAGE_TYPE_FORMAT, baos);
			byte[] imageBytes = baos.toByteArray();

			String blobName = this.barCodeDirectory + toNewYorkZoneYYYYMMDD() + BARCODE_IMAGE_START_NAME
					+ System.currentTimeMillis() + IMAGE_TYPE;
			BlobContainerClient blobContainerClient = this.blobServiceClient
				.getBlobContainerClient(this.blobContainerName);
			BlobClient blobClient = blobContainerClient.getBlobClient(blobName);
			blobClient.upload(new ByteArrayInputStream(imageBytes), imageBytes.length, true);
			return this.sasUrlUtil.generateSasUrl(blobName, STORAGE_URL_TIME_TO_LIVE);
		}
		catch (Exception ex) {
			this.logUtility.logMessage("Error generating barcode: " + ex.getMessage(), ERROR, log);
		}
		return null;
	}

}
