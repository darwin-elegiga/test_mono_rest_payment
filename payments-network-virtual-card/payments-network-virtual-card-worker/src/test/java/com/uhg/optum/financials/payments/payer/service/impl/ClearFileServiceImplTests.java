package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.repository.ClearExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
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
class ClearFileServiceImplTests {

	@Mock
	private ClearExtractRepository repository;

	@Mock
	private VirtualCardsRepository virtualCardsRepository;

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private BlobClient blobClient;

	@InjectMocks
	private ClearFileServiceImpl service;

	private static final String FILE_PATH = "test_clear.csv";

	@Mock
	private LogUtility logUtility;

	@Mock
	FileProcessingStatusLogger fileProcessingStatusLogger;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	/*
	 * @Test void processClearFile_shouldSaveRecords_whenCsvHasValidData() throws
	 * Exception { String[] headers = { "payment_request_id", "network_transaction_id",
	 * "receipt_id", "retrieval_reference_number", "stan", "approval_code",
	 * "system_clear_time", "merchant_clear_time", "bin", "ifi_id", "ifi_name", "card_id",
	 * "card_sequence_number", "payment_network", "masked_card_number", "mti",
	 * "clear_amount", "clear_currency", "billing_amount", "billing_currency",
	 * "geo_indicator", "dcc_indicator", "pos_entry_mode", "pos_transaction_code",
	 * "pin_present_indicator", "pin_valid_indicator", "cvv_present_indicator",
	 * "cvv_valid_indicator", "ecom_indicator", "acs_correlation_id", "eci_indicator",
	 * "clear_action_code", "response_code", "decline_reason", "moto_indicator",
	 * "acquiring_institution_id", "forwarding_institution_id", "merchant_id",
	 * "merchant_terminal_id", "merchant_category_code", "merchant_name", "merchant_city",
	 * "merchant_state", "merchant_country", "payment_code", "payment_code_name",
	 * "channel_code", "channel_code_name" };
	 *
	 * StringWriter sw = new StringWriter(); CSVPrinter printer = new CSVPrinter(sw,
	 * CSVFormat.DEFAULT.withHeader(headers)); printer.printRecord("req123", "txn456",
	 * "rcpt789", "rrn", "stan", "apprCode", "2024-06-01 12:00:00", "2024-06-01 12:00:00",
	 * "123456", "ifiId", "ifiName", "267adc63-aca9-4b74-bdda-c0d25d434340", "1", "VISA",
	 * "****1234", "0100", "100.00", "USD", "100.00", "USD", "geo", "dcc", "pos",
	 * "txnCode", "Y", "Y", "Y", "Y", "E", "acsCorrId", "05", "A", "00",
	 * "Insufficient Funds", "MOTO", "acqInstId", "fwdInstId", "mId", "termId", "6011",
	 * "MerchantName", "City", "State", "US", "PC", "PaymentCodeName", "CH",
	 * "ChannelCodeName"); printer.flush();
	 *
	 * InputStream csvStream = new
	 * ByteArrayInputStream(sw.toString().getBytes(StandardCharsets.UTF_8)); try
	 * (MockedStatic<AzureStorageUtil> utilities = mockStatic(AzureStorageUtil.class)) {
	 * BDDMockito.when(AzureStorageUtil.readBlobItemAsStream(any(),
	 * anyString())).thenReturn(csvStream); utilities.when(() ->
	 * AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, this.FILE_PATH))
	 * .thenReturn(csvStream);
	 *
	 * this.service.processClearFile(this.FILE_PATH); } }
	 */

	@Test
	void processClearFile_shouldNotSave_whenCsvIsEmpty() throws Exception {
		StringWriter sw = new StringWriter();
		CSVPrinter printer = new CSVPrinter(sw,
				CSVFormat.DEFAULT.withHeader("payment_request_id", "system_clear_time"));
		printer.flush();

		InputStream csvStream = new ByteArrayInputStream(sw.toString().getBytes(StandardCharsets.UTF_8));
		try (MockedStatic<AzureStorageUtil> utilities = mockStatic(AzureStorageUtil.class)) {
			BDDMockito.when(AzureStorageUtil.readBlobItemAsStream(any(), anyString())).thenReturn(csvStream);
			utilities.when(() -> AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, this.FILE_PATH))
				.thenReturn(csvStream);
			this.service.processClearFile(this.FILE_PATH);

			verify(this.repository, never()).saveAll(anyList());
		}
	}

	@Test
	void processClearFile_shouldThrowRuntimeException_onIOException() {
		try (MockedStatic<AzureStorageUtil> utilities = mockStatic(AzureStorageUtil.class)) {
			utilities.when(() -> AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, this.FILE_PATH))
				.thenThrow(new RuntimeException(new IOException("Clear extracts file process failed")));

			Assertions.assertThatRuntimeException().isThrownBy(() -> this.service.processClearFile(this.FILE_PATH));
		}
	}

}
