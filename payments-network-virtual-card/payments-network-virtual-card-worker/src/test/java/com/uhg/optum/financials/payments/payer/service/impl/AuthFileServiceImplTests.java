package com.uhg.optum.financials.payments.payer.service.impl;

import com.azure.storage.blob.BlobClient;
import com.azure.storage.blob.BlobContainerClient;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.FileProcessingStatusLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.ModelConversionMapper;
import com.uhg.optum.financials.shared.core.storage.AzureStorageUtil;
import org.apache.commons.csv.CSVFormat;
import org.apache.commons.csv.CSVPrinter;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockedStatic;
import org.mockito.junit.jupiter.MockitoExtension;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.StringWriter;
import java.nio.charset.StandardCharsets;
import java.time.LocalDateTime;
import java.util.UUID;

import static org.mockito.Mockito.any;
import static org.mockito.Mockito.anyList;
import static org.mockito.Mockito.anyString;
import static org.mockito.Mockito.atLeastOnce;
import static org.mockito.Mockito.mockStatic;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;

@ExtendWith(MockitoExtension.class)
class AuthFileServiceImplTests {

	@Mock
	private AuthExtractRepository repository;

	@Mock
	private BlobContainerClient blobContainerClient;

	@Mock
	private BlobClient blobClient;

	@InjectMocks
	private AuthFileServiceImpl service;

	private static final String FILE_PATH = "test_auth.csv";

	@Mock
	private LogUtility logUtility;

	@Mock
	private WebhookClient webhookClient;

	@Mock
	private VirtualCardBalanceService virtualCardBalanceService;

	@Mock
	private AuditLogger auditLogger;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	@Mock
	private ModelConversionMapper cardDetailsMapper;

	@Mock
	private VirtualCardsRepository virtualCardsRepository;

	@Mock
	FileProcessingStatusLogger fileProcessingStatusLogger;

	@Test
	void processAuthFile_shouldSaveRecords_whenCsvHasValidData() throws Exception {
		// List all headers as per your requirement
		String[] headers = { "payment_request_id", "network_transaction_id", "receipt_id", "original_auth_id",
				"retrieval_reference_number", "stan", "approval_code", "system_auth_time", "merchant_auth_time",
				"auth_expiry_time", "bin", "ifi_id", "ifi_name", "card_id", "card_sequence_number", "payment_network",
				"masked_card_number", "mti", "auth_amount", "auth_currency", "billing_amount", "billing_currency",
				"geo_indicator", "dcc_indicator", "pos_entry_mode", "pos_transaction_code", "pin_present_indicator",
				"pin_valid_indicator", "cvv2_present_indicator", "cvv2_valid_indicator", "ecom_indicator",
				"acs_correlation_id", "eci_indicator", "auth_action_code", "response_code", "decline_reason",
				"moto_indicator", "acquiring_institution_id", "forwarding_institution_id", "merchant_id",
				"merchant_terminal_id", "merchant_category_code", "merchant_name", "merchant_city", "merchant_state",
				"merchant_country", "payment_code", "payment_code_name", "channel_code", "channel_code_name" };

		StringWriter sw = new StringWriter();
		CSVPrinter printer = new CSVPrinter(sw, CSVFormat.DEFAULT.withHeader(headers));
		// Provide sample values for each header (use dummy or null/empty as needed)
		printer.printRecord("req123", "txn456", "rcpt789", "origAuthId", "rrn", "stan", "apprCode",
				"2024-06-01 12:00:00", "2024-06-01 12:00:00", "2024-06-02 12:00:00", "123456", "ifiId", "ifiName",
				"267adc63-aca9-4b74-bdda-c0d25d434340", "1", "VISA", "****1234", "0100", "100.00", "USD", "100.00",
				"USD", "geo", "dcc", "pos", "txnCode", "Y", "Y", "Y", "Y", "E", "acsCorrId", "05", "A", "00",
				"Insufficient Funds", "MOTO", "acqInstId", "fwdInstId", "mId", "termId", "6011", "MerchantName", "City",
				"State", "US", "PC", "PaymentCodeName", "CH", "ChannelCodeName");
		printer.flush();

		InputStream csvStream = new ByteArrayInputStream(sw.toString().getBytes(StandardCharsets.UTF_8));
		try (MockedStatic<AzureStorageUtil> utilities = mockStatic(AzureStorageUtil.class)) {
			BDDMockito.when(AzureStorageUtil.readBlobItemAsStream(any(), anyString())).thenReturn(csvStream);
			utilities.when(() -> AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, this.FILE_PATH))
				.thenReturn(csvStream);

			this.service.processAuthFile(this.FILE_PATH);
		}
	}

	@Test
	void processAuthFile_shouldNotSave_whenCsvIsEmpty() throws Exception {
		StringWriter sw = new StringWriter();
		CSVPrinter printer = new CSVPrinter(sw, CSVFormat.DEFAULT.withHeader("payment_request_id", "system_auth_time"));
		printer.flush();

		InputStream csvStream = new ByteArrayInputStream(sw.toString().getBytes(StandardCharsets.UTF_8));
		try (MockedStatic<AzureStorageUtil> utilities = mockStatic(AzureStorageUtil.class)) {
			BDDMockito.when(AzureStorageUtil.readBlobItemAsStream(any(), anyString())).thenReturn(csvStream);
			utilities.when(() -> AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, this.FILE_PATH))
				.thenReturn(csvStream);
			this.service.processAuthFile(this.FILE_PATH);

			verify(this.repository, never()).saveAll(anyList());
		}
	}

	@Test
	void processAuthFile_shouldThrowRuntimeException_onIOException() {
		try (MockedStatic<AzureStorageUtil> utilities = mockStatic(AzureStorageUtil.class)) {
			utilities.when(() -> AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, this.FILE_PATH))
				.thenThrow(new RuntimeException(new IOException("Auth extracts file process failed")));

			Assertions.assertThatRuntimeException().isThrownBy(() -> this.service.processAuthFile(this.FILE_PATH));
		}
	}

	@Test
	void processAuthFile_shouldPreserveClearingStatus_whenUpdatingExistingAuthExtract() throws Exception {
		String[] headers = { "payment_request_id", "network_transaction_id", "receipt_id", "original_auth_id",
				"retrieval_reference_number", "stan", "approval_code", "system_auth_time", "merchant_auth_time",
				"auth_expiry_time", "bin", "ifi_id", "ifi_name", "card_id", "card_sequence_number", "payment_network",
				"masked_card_number", "mti", "auth_amount", "auth_currency", "billing_amount", "billing_currency",
				"geo_indicator", "dcc_indicator", "pos_entry_mode", "pos_transaction_code", "pin_present_indicator",
				"pin_valid_indicator", "cvv2_present_indicator", "cvv2_valid_indicator", "ecom_indicator",
				"acs_correlation_id", "eci_indicator", "auth_action_code", "response_code", "decline_reason",
				"moto_indicator", "acquiring_institution_id", "forwarding_institution_id", "merchant_id",
				"merchant_terminal_id", "merchant_category_code", "merchant_name", "merchant_city", "merchant_state",
				"merchant_country", "payment_code", "payment_code_name", "channel_code", "channel_code_name" };

		String receiptId = "rcpt-preserve-1";
		String cardId = UUID.randomUUID().toString();

		StringWriter sw = new StringWriter();
		CSVPrinter printer = new CSVPrinter(sw, CSVFormat.DEFAULT.withHeader(headers));
		printer.printRecord("req123", "txn456", receiptId, "origAuthId", "rrn", "stan", "apprCode",
				"2024-06-01T12:00:00Z", "2024-06-01T12:00:00Z", "2024-06-02T12:00:00Z", "123456", "ifiId", "ifiName",
				cardId, "1", "VISA", "****1234", "0100", "100.00", "USD", "100.00", "USD", "geo", "dcc", "pos",
				"txnCode", "Y", "Y", "Y", "Y", "E", "acsCorrId", "05", "DECLINED", "05", "Insufficient Funds", "MOTO",
				"acqInstId", "fwdInstId", "mId", "termId", "6011", "MerchantName", "City", "State", "US", "PC",
				"PaymentCodeName", "CH", "ChannelCodeName");
		printer.flush();

		AuthExtract existing = new AuthExtract();
		existing.setReceiptId(receiptId);
		existing.setCardId(UUID.fromString(cardId));
		existing.setCreatedBy("existing-created-by");
		existing.setCreatedAt(LocalDateTime.now().minusDays(1));
		existing.setTransactionStatus("TX_STATUS");
		existing.setTransactionType("TX_TYPE");
		existing.setClearingStatus("AUTH_CLEAR");

		BDDMockito.when(this.virtualCardsRepository.existsByCardGuid(UUID.fromString(cardId))).thenReturn(true);
		BDDMockito.when(this.repository.findByReceiptId(receiptId)).thenReturn(existing);
		BDDMockito.when(this.cardDetailsMapper.modelMapper(any(AuthExtract.class), BDDMockito.eq(AuthExtract.class)))
			.thenAnswer((invocation) -> invocation.getArgument(0));

		InputStream csvStream = new ByteArrayInputStream(sw.toString().getBytes(StandardCharsets.UTF_8));
		try (MockedStatic<AzureStorageUtil> utilities = mockStatic(AzureStorageUtil.class)) {
			BDDMockito.when(AzureStorageUtil.readBlobItemAsStream(any(), anyString())).thenReturn(csvStream);
			utilities.when(() -> AzureStorageUtil.readBlobItemAsStream(this.blobContainerClient, this.FILE_PATH))
				.thenReturn(csvStream);

			this.service.processAuthFile(this.FILE_PATH);
		}

		verify(this.repository, atLeastOnce()).saveAll(BDDMockito.argThat((iterable) -> {
			for (Object item : iterable) {
				if (item instanceof AuthExtract auth && receiptId.equals(auth.getReceiptId())) {
					return "AUTH_CLEAR".equals(auth.getClearingStatus());
				}
			}
			return false;
		}));
	}

}
