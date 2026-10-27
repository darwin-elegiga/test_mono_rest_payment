
package com.uhg.optum.financials.payments.payer.utils;

import com.azure.storage.blob.BlobContainerClient;
import com.azure.storage.blob.BlobServiceClient;
import com.uhg.optum.financials.shared.core.storage.AzureStorageUtil;
import lombok.extern.slf4j.Slf4j;
import org.apache.commons.io.FilenameUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.util.Arrays;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.stream.Collectors;

@Service
@Slf4j
public class CsvFileWriter {

	private static final String LOCAL_WORK_DIR = "payments-network-virtual-card";

	private static final String LOCAL_INBOUND_DIR = "inbound";

	@Autowired
	@Qualifier("defaultBlobServiceClient")
	private BlobServiceClient blobServiceClient;

	@Value("${storage.containerName}")
	private String containerName;

	@Value("${directory.auth-extract-file}")
	private String inboundDir;

	public Path buildWorkingFilePath(String fileName) {
		return Path.of(System.getProperty("java.io.tmpdir"), LOCAL_WORK_DIR, LOCAL_INBOUND_DIR, fileName);
	}

	public void appendRow(Path csvPath, Map<String, String> row) throws IOException {
		Objects.requireNonNull(row, "row map must not be null");
		Files.createDirectories(csvPath.getParent());

		if (Files.notExists(csvPath)) {
			String header = String.join(",", row.keySet());
			Files.writeString(csvPath, header + System.lineSeparator(), StandardOpenOption.CREATE,
					StandardOpenOption.WRITE);
		}

		String headerLine;
		try (var stream = Files.lines(csvPath, StandardCharsets.UTF_8)) {
			headerLine = stream.findFirst().orElse("");
		}
		String[] headers = headerLine.isEmpty() ? row.keySet().toArray(new String[0]) : headerLine.split(",");

		List<String> orderedValues = Arrays.stream(headers)
			.map((h) -> row.getOrDefault(h, ""))
			.map(CsvFileWriter::escapeCsv)
			.collect(Collectors.toList());

		String csvLine = String.join(",", orderedValues) + System.lineSeparator();
		Files.write(csvPath, csvLine.getBytes(StandardCharsets.UTF_8), StandardOpenOption.CREATE,
				StandardOpenOption.APPEND);
		storeFileInAzureBlobStorage(csvPath, Files.readString(csvPath));
	}

	private static String escapeCsv(String value) {
		if (value == null) {
			return "";
		}
		boolean mustQuote = value.contains(",") || value.contains("\"") || value.contains("\n") || value.contains("\r");
		String escaped = value.replace("\"", "\"\"");
		return (mustQuote) ? ("\"" + escaped + "\"") : escaped;
	}

	private void storeFileInAzureBlobStorage(Path csvPath, String csvFileContent) {

		BlobContainerClient blobContainerClient = this.blobServiceClient
			.createBlobContainerIfNotExists(this.containerName);

		String fileNameWithPath = this.inboundDir + FilenameUtils.getName(csvPath.getFileName().toString());
		AzureStorageUtil.createBlobItem(blobContainerClient, fileNameWithPath, csvFileContent.getBytes());
		log.info("Uploaded file to Azure Blob Storage: {}", fileNameWithPath);
		// Clean up local file after upload
		deleteLocalFile(csvPath);

	}

	private void deleteLocalFile(Path csvPath) {
		try {
			Files.deleteIfExists(csvPath);
			log.info("Deleted local file: {}", csvPath);
		}
		catch (IOException ex) {
			log.error("Failed to delete local file: {}", csvPath, ex);
		}
	}

	public void writeToCsvFile(Path csvPath, Map<String, String> row) throws IOException {
		writeToCsvFile(csvPath, row, true);
	}

	private void writeToCsvFile(Path csvPath, Map<String, String> row, boolean uploadAfterWrite) throws IOException {
		Objects.requireNonNull(row, "row map must not be null");
		Files.createDirectories(csvPath.getParent());

		boolean fileExists = Files.exists(csvPath);
		String[] headers;
		if (!fileExists) {
			headers = row.keySet().toArray(new String[0]);
		}
		else {
			try (var reader = Files.newBufferedReader(csvPath, StandardCharsets.UTF_8)) {
				String headerLine = reader.readLine();
				headers = (headerLine != null) ? headerLine.split(",") : row.keySet().toArray(new String[0]);
			}
		}

		try (var writer = Files.newBufferedWriter(csvPath, StandardCharsets.UTF_8, StandardOpenOption.CREATE,
				StandardOpenOption.APPEND);
				var csvPrinter = new org.apache.commons.csv.CSVPrinter(writer,
						fileExists ? org.apache.commons.csv.CSVFormat.DEFAULT
								: org.apache.commons.csv.CSVFormat.DEFAULT.withHeader(headers))) {
			List<String> orderedValues = Arrays.stream(headers)
				.map((h) -> row.getOrDefault(h, ""))
				.collect(Collectors.toList());
			csvPrinter.printRecord(orderedValues);
		}

		if (uploadAfterWrite) {
			storeFileInAzureBlobStorage(csvPath, Files.readString(csvPath));
		}
	}

	public void writeRowsToCsvFile(Path csvPath, List<Map<String, String>> rows) throws IOException {
		Objects.requireNonNull(rows, "rows list must not be null");
		if (rows.isEmpty()) {
			throw new IllegalArgumentException("rows list must not be empty");
		}

		Files.deleteIfExists(csvPath);
		for (Map<String, String> row : rows) {
			writeToCsvFile(csvPath, row, false);
		}

		storeFileInAzureBlobStorage(csvPath, Files.readString(csvPath));
	}

}
