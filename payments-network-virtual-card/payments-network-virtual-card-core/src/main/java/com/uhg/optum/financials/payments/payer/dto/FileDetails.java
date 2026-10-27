package com.uhg.optum.financials.payments.payer.dto;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class FileDetails {

	private String fileId;

	private String fileName;

	private String backupFileName;

	private String fileGenerationTimeStamp;

}
