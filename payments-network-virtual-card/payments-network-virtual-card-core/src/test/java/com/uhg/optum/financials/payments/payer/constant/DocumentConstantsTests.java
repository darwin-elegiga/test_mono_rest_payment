package com.uhg.optum.financials.payments.payer.constant;

import org.junit.jupiter.api.Test;
import static org.assertj.core.api.Assertions.assertThat;

class DocumentConstantsTests {

	@Test
	void allConstants_shouldHaveExpectedValues() {
		assertThat(DocumentConstants.LOADING).isEqualTo("loading");
		assertThat(DocumentConstants.PAYMENT).isEqualTo("payment");
		assertThat(DocumentConstants.SETTLED).isEqualTo("settled");
		assertThat(DocumentConstants.DOT_DELIMITER).isEqualTo(".");
		assertThat(DocumentConstants.SLASH_DELIMITER).isEqualTo("/");
		assertThat(DocumentConstants.UNDERSCORE_DELIMITER).isEqualTo("_");
		assertThat(DocumentConstants.SUCCESS_FILE_EXTENSION).isEqualTo(".success");
		assertThat(DocumentConstants.TXT_FILE_EXTENSION).isEqualTo(".txt");
		assertThat(DocumentConstants.CTL_FILE_EXTENSION).isEqualTo(".ctl");
		assertThat(DocumentConstants.GZIP_FILE_EXTENSION).isEqualTo(".gz");
		assertThat(DocumentConstants.AP_SUCCESS_FILE_REGEX).isEqualTo("[A-Z0-9]+_AP[.]success");
		assertThat(DocumentConstants.AP_TXT_FILE_REGEX).isEqualTo("[A-Z0-9]+_AP[0-9]{14}[.]txt");
		assertThat(DocumentConstants.REAP_SUCCESS_FILE_REGEX).isEqualTo("[A-Z0-9]+_REAP[.]success");
		assertThat(DocumentConstants.REAP_TXT_FILE_REGEX).isEqualTo("[A-Z0-9]+_REAP[0-9]{14}[.]txt");
		assertThat(DocumentConstants.CONSOLIDATED_835_FILE_REGEX).isEqualTo("[A-Z0-9]+_[0-9]{8}[.]txt");
		assertThat(DocumentConstants.TRN_REGEX).isEqualTo("~TRN\\*\\d+\\*([A-Z0-9_]+)\\*");
		assertThat(DocumentConstants.COMPLETED).isEqualTo("completed");
		assertThat(DocumentConstants.FAILED).isEqualTo("failed");
		assertThat(DocumentConstants.DENIED).isEqualTo("denied");
		assertThat(DocumentConstants.ERROR_CODE_3024).isEqualTo("ERR3024");
		assertThat(DocumentConstants.ACTIVITY).isEqualTo("activity");
		assertThat(DocumentConstants.DUPLICATE_PAYMENT_EXCEPTION_MESSAGE)
			.isEqualTo("payment identifier already exists");
		assertThat(DocumentConstants.TENANT_HEADER).isEqualTo("X-Optum-Tenant-Id");
		assertThat(DocumentConstants.BLOBS_PATH).isEqualTo("blobs/");
		assertThat(DocumentConstants.SIXTY_FIVE).isEqualTo("65");
		assertThat(DocumentConstants.REM_ADV).isEqualTo("rem-adv");
		assertThat(DocumentConstants.JOB_RUNNING_MSG).isEqualTo("Job Already Running");
		assertThat(DocumentConstants.ERROR_SUBMITTING_JOB_MSG).isEqualTo("Error submitting job.");
		assertThat(DocumentConstants.SUMMARY).isEqualTo("Summary_");
		assertThat(DocumentConstants.C835_ERROR).isEqualTo("Error occurred while processing C835 with file path: ");
		assertThat(DocumentConstants.AP_ERROR).isEqualTo("Error occurred while processing AP with file path: ");
		assertThat(DocumentConstants.REAP_ERROR).isEqualTo("Error occurred while processing REAP with file path: ");
		assertThat(DocumentConstants.AP_MESSAGE_ERROR).isEqualTo("Error occurred while processing AP Message ");
		assertThat(DocumentConstants.REAP_MESSAGE_ERROR).isEqualTo("Error occurred while processing REAP Message ");
		assertThat(DocumentConstants.SUMMARY_DOCUMENT_MESSAGE_ERROR)
			.isEqualTo("Error occurred while generating summary document for network payment id: ");
		assertThat(DocumentConstants.ERROR_MSG).isEqualTo(" Error msg: ");
		assertThat(DocumentConstants.COMPANION_FILE).isEqualTo("companionFile");
		assertThat(DocumentConstants.JSON_EXTENSION).isEqualTo(".json");
	}

}
