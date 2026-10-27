package com.uhg.optum.financials.payments.payer.dto;

import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.Getter;
import lombok.Setter;

import java.time.LocalDateTime;

@Getter
@Setter
@JsonInclude(JsonInclude.Include.NON_NULL)
public class FileProcessingStatusDto {

	private String fileName;

	private String fileType;

	private String status;

	private Integer recordCount;

	private String errorMessage;

	private LocalDateTime processedAt;

	private String processedBy;

	private String recordErrors;

}
