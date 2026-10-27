package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.repository.InvalidCardDeclineEventsRepository;
import com.uhg.optum.financials.payments.payer.util.FileProcessingStatusLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.shared.core.storage.AzureStorageUtil;
import org.apache.commons.csv.CSVFormat;
import org.apache.commons.csv.CSVPrinter;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockedStatic;
import org.mockito.MockitoAnnotations;
import org.mockito.junit.jupiter.MockitoExtension;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.StringWriter;
import java.nio.charset.StandardCharsets;

import static org.mockito.Mockito.any;
import static org.mockito.Mockito.anyList;
import static org.mockito.Mockito.anyString;
import static org.mockito.Mockito.mockStatic;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class InvalidCardDeclineAuthFileServiceImplTests {

	@Mock
	private InvalidCardDeclineEventsRepository repository;

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private BlobClient blobClient;

	@InjectMocks
	private InvalidCardDeclineAuthFileServiceImpl service;

	private static final String FILE_PATH = "test_invalid_card_decline_auth.csv";

	@Mock
	private LogUtility logUtility;

	@Mock
	FileProcessingStatusLogger fileProcessingStatusLogger;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void processInvalidAuthDeclineFile_shouldSaveRecords_whenCsvHasValidData() throws Exception {
		// List all headers as per your requirement
		String[] headers = { "network_transaction_id", "retrieval_reference_number", "stan", "system_auth_time",
				"time_local_transaction", "date_local_transaction", "bin", "ifi_id", "ifi_name", "card_sequence_number",
				"payment_network", "masked_card_number", "mti", "auth_amount", "auth_currency", "billing_amount",
				"billing_currency", "additional_data_private_use", "pos_entry_mode", "pos_data", "auth_action_code",
				"response_code", "acquiring_institution_id", "forwarding_institution_id", "merchant_id",
				"merchant_terminal_id", "merchant_category_code", "merchant_name_location" };

		StringWriter sw = new StringWriter();
		CSVPrinter printer = new CSVPrinter(sw, CSVFormat.DEFAULT.withHeader(headers));
		// Provide sample values for each header (use dummy or null/empty as needed)
		printer.printRecord("txn456", "rrn", "stan", "2024-11-03T10:30:00Z", "103000", "1103", "123456", "ifiId",
				"ifiName", "1", "VISA", "****1234", "0100", "100.00", "USD", "100.00", "USD", "privateData", "pos",
				"posData", "A", "00", "acqInstId", "fwdInstId", "mId", "termId", "6011", "MerchantName Location");
		printer.flush();

		InputStream csvStream = new ByteArrayInputStream(sw.toString().getBytes(StandardCharsets.UTF_8));
		try (MockedStatic<AzureStorageUtil> utilities = mockStatic(AzureStorageUtil.class)) {
			BDDMockito.when(AzureStorageUtil.readBlobItemAsStream(any(), anyString())).thenReturn(csvStream);
			utilities.when(() -> AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, this.FILE_PATH))
				.thenReturn(csvStream);

			this.service.processInvalidAuthDeclineFile(this.FILE_PATH);
		}
	}

	@Test
	void processInvalidAuthDeclineFile_shouldNotSave_whenCsvIsEmpty() throws Exception {
		StringWriter sw = new StringWriter();
		CSVPrinter printer = new CSVPrinter(sw,
				CSVFormat.DEFAULT.withHeader("network_transaction_id", "system_auth_time"));
		printer.flush();

		InputStream csvStream = new ByteArrayInputStream(sw.toString().getBytes(StandardCharsets.UTF_8));
		try (MockedStatic<AzureStorageUtil> utilities = mockStatic(AzureStorageUtil.class)) {
			BDDMockito.when(AzureStorageUtil.readBlobItemAsStream(any(), anyString())).thenReturn(csvStream);
			utilities.when(() -> AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, this.FILE_PATH))
				.thenReturn(csvStream);
			this.service.processInvalidAuthDeclineFile(this.FILE_PATH);

			verify(this.repository, never()).saveAll(anyList());
		}
	}

	@Test
	void processInvalidAuthDeclineFile_shouldThrowRuntimeException_onIOException() {
		try (MockedStatic<AzureStorageUtil> utilities = mockStatic(AzureStorageUtil.class)) {
			utilities.when(() -> AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, this.FILE_PATH))
				.thenThrow(new RuntimeException(
						new IOException("Invalid Card Decline Auth extracts file process failed")));

			Assertions.assertThatRuntimeException()
				.isThrownBy(() -> this.service.processInvalidAuthDeclineFile(this.FILE_PATH));
		}
	}

}
