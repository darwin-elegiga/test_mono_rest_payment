package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.dto.reports.AuthorizedFileReportDto;
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

import static com.uhg.optum.financials.payments.payer.constant.JobConstant.PIPE_DELIMITER;

@Service
@Slf4j
public class AuthorizedFileCreationServiceImpl implements VCCReportsFileService {

	private static final String HEADER = "HEADER";

	private static final String PROCESSOR_NAME = "OPTUM CHOICE";

	private static final String REPORT_NAME = "AUTHORIZED";

	private static final String TRAILER = "TRAILER";

	private static final String BACK_SLASH = "/";

	private static final DateTimeFormatter FILE_NAME_FORMAT = DateTimeFormatter.ofPattern("yyyyMMddHHmmss");

	private static final DateTimeFormatter FILE_DATE_FORMAT = DateTimeFormatter.ofPattern("MMddyyyy");

	private final VCCReportsRepository vccReportsRepository;

	private final FileTransferUtil fileTransferUtil;

	private final ECGTransferUtil ecgTransferUtil;

	private final BlobContainerClient blobContainerClient;

	@Value("${directory.file.authorized-file-report-path}")
	private String authorizedFileReportPath;

	@Value("${directory.file.work.dir}")
	private String workDir;

	public AuthorizedFileCreationServiceImpl(VCCReportsRepository vccReportsRepository,
			FileTransferUtil fileTransferUtil, ECGTransferUtil ecgTransferUtil,
			BlobContainerClient blobContainerClient) {
		this.vccReportsRepository = vccReportsRepository;
		this.fileTransferUtil = fileTransferUtil;
		this.ecgTransferUtil = ecgTransferUtil;
		this.blobContainerClient = blobContainerClient;
	}

	@Override
	public String createFile() throws IOException {

		log.info("Started authorized file generation process.");
		List<AuthorizedFileReportDto> authorizedFileReportDetails = this.vccReportsRepository
			.findAuthorizedFileReportDetails();

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

		try (BufferedWriter writer = Files.newBufferedWriter(file.toPath())) {
			writeHeader(writer, fileDate, runDate);
			for (AuthorizedFileReportDto row : authorizedFileReportDetails) {
				writer.write(generateDetailRow(row, fileName));
				writer.newLine();
				rowCount++;
			}
			writeTrailer(writer, rowCount);
		}
		log.info("Authorized file generated: {}", filePath);
		this.fileTransferUtil.uploadFile(this.blobContainerClient, file, this.authorizedFileReportPath + fileName);
		this.ecgTransferUtil.transferToECG(file);

		log.info("File place to Blob and ECG transfer completed for authorized file: {}", fileName);
		// Delete the file from /tmp after all tasks
		fileCleanUp(file);

		return fileName;

	}

	private String generateDetailRow(AuthorizedFileReportDto row, String fileName) {
		String[] fields = { formatField(row.getCardNumber(), 30), formatField(row.getTransactionDate(), 17),
				formatField(row.getTransactionCurrencyCode(), 3),
				formatField(String.valueOf(row.getAddressVerificationResponse()), 30),
				formatField(row.getAuthorizationResponse(), 30),
				formatField(String.valueOf(row.getAuthorizationAmount()), 13),
				formatField(row.getAuthorizationCode(), 10), formatField(row.getNetworkCode(), 30),
				formatField(row.getMerchantNumber(), 30), formatField(row.getMerchantName(), 50),
				formatField(row.getMerchantCategoryCode(), 4),
				formatField((row.getMerchantCountryCode() != null && row.getMerchantCountryCode().length() >= 2)
						? row.getMerchantCountryCode().substring(0, 2) : "US", 5),
				formatField(row.getSeExternalIdNumber(), 16), formatField(row.getBin(), 6), row.getTpa(), fileName };
		return String.join(PIPE_DELIMITER, fields);
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

	private void writeHeader(BufferedWriter writer, String fileDate, String runDate) throws IOException {
		writer.write(String.join(PIPE_DELIMITER, HEADER, PROCESSOR_NAME, REPORT_NAME, fileDate, runDate, runDate));
		writer.newLine();

	}

	private void writeTrailer(BufferedWriter writer, int totalRecords) throws IOException {
		writer.write(String.join(PIPE_DELIMITER, TRAILER, String.format("%09d", totalRecords)));
		writer.newLine();
	}

	private static String generateFileName(String timestamp) {
		return timestamp + "_Optum_authorized_op_debit.TXT";
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
