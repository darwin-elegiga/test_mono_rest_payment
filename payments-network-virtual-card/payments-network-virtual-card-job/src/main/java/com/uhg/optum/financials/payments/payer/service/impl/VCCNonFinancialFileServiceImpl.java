package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.dto.reports.VCCNonFinancialReportDto;
import com.uhg.optum.financials.payments.payer.model.BillingAddress;
import com.uhg.optum.financials.payments.payer.model.CardHolder;
import com.uhg.optum.financials.payments.payer.repository.CardholderRepository;
import com.uhg.optum.financials.payments.payer.repository.VCCReportsRepository;
import com.uhg.optum.financials.payments.payer.service.VCCReportsFileService;
import com.uhg.optum.financials.payments.payer.util.ECGTransferUtil;
import com.uhg.optum.financials.payments.payer.util.FileTransferUtil;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.io.FilenameUtils;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.io.BufferedWriter;
import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.List;

@Service
@Slf4j
public class VCCNonFinancialFileServiceImpl implements VCCReportsFileService {

	private static final String BACK_SLASH = "/";

	@Value("${directory.file.non-financial-report-path}")
	private String nonFinancialReportPath;

	@Value("${directory.file.work.dir}")
	private String workDir;

	private static final String HEADER = "HEADER";

	private static final String PROCESSOR_NAME = "OPTUM CHOICE";

	private static final String REPORT_NAME = "NON-FINANCIAL";

	private static final String FILE_FORMAT = "3";

	private static final String TRAILER = "TRAILER";

	private static final DateTimeFormatter FILE_NAME_FORMAT = DateTimeFormatter.ofPattern("yyyyMMddHHmmss");

	private static final DateTimeFormatter FILE_DATE_FORMAT = DateTimeFormatter.ofPattern("MMddyyyy");

	private final VCCReportsRepository vccReportsRepository;

	private final CardholderRepository cardholderRepository;

	private final FileTransferUtil fileTransferUtil;

	private final ECGTransferUtil ecgTransferUtil;

	private final BlobContainerClient blobContainerClient;

	public VCCNonFinancialFileServiceImpl(VCCReportsRepository vccReportsRepository,
			CardholderRepository cardholderRepository, FileTransferUtil fileTransferUtil,
			ECGTransferUtil ecgTransferUtil, BlobContainerClient blobContainerClient) {
		this.vccReportsRepository = vccReportsRepository;
		this.cardholderRepository = cardholderRepository;
		this.fileTransferUtil = fileTransferUtil;
		this.ecgTransferUtil = ecgTransferUtil;
		this.blobContainerClient = blobContainerClient;
	}

	@Override
	public String createFile() throws IOException {
		List<VCCNonFinancialReportDto> data = this.vccReportsRepository.findReportRowsByPaymentDetailsId();
		int rowCount = 0;
		String timestamp = LocalDateTime.now().format(FILE_NAME_FORMAT);
		String fileDate = LocalDateTime.now().format(FILE_DATE_FORMAT);
		String runDate = LocalDateTime.now().minusDays(1).format(FILE_DATE_FORMAT);
		String fileName = generateFileName(timestamp);
		String filePath;
		File file;

		filePath = !this.workDir.startsWith(BACK_SLASH) ? FilenameUtils.getPath(this.workDir)
				: BACK_SLASH + FilenameUtils.getPath(this.workDir);

		file = new File(filePath, FilenameUtils.getName(fileName));

		CardHolder existingCardHolder = this.cardholderRepository.findCardholder();

		try (BufferedWriter writer = Files.newBufferedWriter(file.toPath())) {
			writeHeader(writer, fileDate, runDate);
			for (VCCNonFinancialReportDto row : data) {
				writer.write(generateDetailRow(row, existingCardHolder, timestamp));
				writer.newLine();
				rowCount++;
			}
			writeTrailer(writer, rowCount);
		}
		log.info("Non Financial file generated: {}", filePath);
		this.fileTransferUtil.uploadFile(this.blobContainerClient, file, this.nonFinancialReportPath + fileName);
		this.ecgTransferUtil.transferToECG(file);

		// Delete the file from /tmp after all tasks
		fileCleanUp(file);

		return fileName;
	}

	private void writeHeader(BufferedWriter writer, String fileDate, String runDate) throws IOException {
		writer.write(String.join("|", HEADER, PROCESSOR_NAME, REPORT_NAME, fileDate, runDate, runDate, FILE_FORMAT));
		writer.newLine();
	}

	private void writeTrailer(BufferedWriter writer, int rowCount) throws IOException {
		writer.write(String.join("|", TRAILER, String.format("%09d", rowCount)));
		writer.newLine();
	}

	private String generateFileName(String timestamp) {
		return timestamp + "_OptumChoice_nonfinancial_op_debit.TXT";
	}

	private String generateDetailRow(VCCNonFinancialReportDto row, CardHolder existingCardHolder, String timestamp) {
		BillingAddress billingAddress = existingCardHolder.getBillingAddress();
		String[] fields = { formatField(row.getMaskedPan(), 30), formatField(row.getCreatedDate(), 8),
				formatField(row.getExpiryDate(), 8), formatField("", 1), formatField("", 88),
				formatField(existingCardHolder.getFirstName(), 50), formatField(existingCardHolder.getLastName(), 50),
				formatField(billingAddress.getLine1(), 100), formatField("", 100),
				formatField(billingAddress.getCity(), 50), formatField("", 2),
				formatField(billingAddress.getPostalCode(), 9), formatField(billingAddress.getCountry(), 2),
				formatField((existingCardHolder.getPhoneNumber() != null)
						? existingCardHolder.getPhoneNumber().replaceAll("[^\\d]", "") : "", 10),
				formatField("", 10), formatField(row.getStatus(), 50),
				formatField(String.valueOf(row.getCurrentBalance()), 13),
				formatField(String.valueOf(row.getCurrentBalance()).startsWith("-") ? "-" : "+", 1),
				formatField("", 20), formatField("", 20), formatField("", 8), formatField("", 30), formatField("", 30),
				formatField("", 100), formatField("", 6), formatField(String.valueOf(row.getAvailableBalance()), 13),
				formatField("", 13), formatField("", 3), formatField("", 13), formatField("", 8), formatField("", 8),
				formatField("", 8), formatField(row.getIssuerPaymentId(), 16),
				formatField((row.getMaskedPan() != null && row.getMaskedPan().length() >= 6)
						? row.getMaskedPan().substring(0, 6) : "", 6),
				row.getOrgAccountSubaccount(), generateFileName(timestamp) };
		return String.join("|", fields);
	}

	private String formatField(String value, int length) {
		if (value == null) {
			value = "";
		}
		if (value.length() > length) {
			return value.substring(0, length);
		}
		else {
			return String.format("%-" + length + "s", value).trim();
		}
	}

	private void fileCleanUp(File file) {
		try {
			Files.deleteIfExists(file.toPath());
			log.info("Temporary file deleted: {}", file.getAbsolutePath());
		}
		catch (IOException ex) {
			log.warn("Failed to delete temporary file: {}", file.getAbsolutePath(), ex);
		}
	}

}
