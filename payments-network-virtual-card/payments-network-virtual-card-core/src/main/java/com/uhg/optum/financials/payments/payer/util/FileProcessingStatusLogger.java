package com.uhg.optum.financials.payments.payer.util;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.model.FileProcessingStatus;
import com.uhg.optum.financials.payments.payer.repository.FileProcessingStatusRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.time.LocalDateTime;
import java.util.Map;

@Component
@RequiredArgsConstructor
@Slf4j
public class FileProcessingStatusLogger {

	private final FileProcessingStatusRepository fileProcessingStatusRepository;

	public void logFileProcessed(String fileName, String fileType, Integer recordCount, Map<String, String> errorMap) {
		FileProcessingStatus audit = new FileProcessingStatus();
		audit.setFileName(fileName);
		audit.setFileType(fileType);
		audit.setRecordCount(recordCount);
		audit.setProcessedAt(LocalDateTime.now());
		audit.setProcessedBy(fileType + "_FILE_EXTRACT_PROCESSOR");
		if (recordCount == null || recordCount == 0) {
			audit.setErrorMessage("File is Empty or No valid records found");
		}
		if (errorMap != null && !errorMap.isEmpty()) {
			try {
				audit.setStatus("PARTIAL_SUCCESS");
				audit.setErrorMessage("Partial errors occurred");
				audit.setRecordErrors(new ObjectMapper().writeValueAsString(errorMap));
			}
			catch (JsonProcessingException ex) {
				log.error("Failed to convert errorMap to JSON", ex);
			}
		}
		else {
			audit.setStatus("SUCCESS");
		}
		this.fileProcessingStatusRepository.save(audit);
		log.info("File processed successfully: {} | Type: {} | Records: {} | Errors: {}", fileName, fileType,
				recordCount, ((errorMap != null) ? errorMap.size() : 0));
	}

	public void logFileFailed(String fileName, String fileType, int recordCount, String errorMessage,
			Map<String, String> errorMap) {
		FileProcessingStatus audit = new FileProcessingStatus();
		audit.setFileName(fileName);
		audit.setFileType(fileType);
		audit.setStatus("FAILED");
		audit.setRecordCount(recordCount);
		audit.setErrorMessage(errorMessage);
		audit.setProcessedAt(LocalDateTime.now());
		audit.setProcessedBy(fileType + "_FILE_EXTRACT_PROCESSOR");
		if (errorMap != null && !errorMap.isEmpty()) {
			try {
				audit.setRecordErrors(new ObjectMapper().writeValueAsString(errorMap));
			}
			catch (JsonProcessingException ex) {
				log.error("Failed to convert errorMap to JSON", ex);
			}
		}
		this.fileProcessingStatusRepository.save(audit);
		log.error("File processing failed: {} | Type: {} | Error: {} | Errors: {}", fileName, fileType, errorMessage,
				((errorMap != null) ? errorMap.size() : 0));
	}

}
