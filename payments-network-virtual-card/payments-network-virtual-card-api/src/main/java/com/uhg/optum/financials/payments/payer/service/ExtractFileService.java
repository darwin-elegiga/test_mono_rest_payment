package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.FileProcessingStatusDto;

import java.util.List;

public interface ExtractFileService {

	List<FileProcessingStatusDto> getFileProcessingStatusByDate(String date);

}
