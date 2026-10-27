package com.uhg.optum.financials.payments.payer.job.writer;

import com.azure.storage.blob.specialized.AppendBlobClient;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.CompanionDto;
import com.uhg.optum.financials.payments.payer.exception.CompanionFileCreationException;
import com.uhg.optum.financials.payments.payer.repository.PaymentRequestRepository;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.SasUrlUtil;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.batch.infrastructure.item.Chunk;

import java.io.ByteArrayInputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.spy;

@ExtendWith(MockitoExtension.class)
public class CompanionDataWriterTests {

	@Mock
	private AppendBlobClient appendBlobClient;

	@Mock
	private SasUrlUtil sasUrlUtil;

	@Mock
	private LogUtility logUtility;

	@Mock
	private WebhookClient webhookClient;

	@Mock
	private PaymentRequestRepository paymentRequestRepository;

	private CompanionDataWriter writer;

	private final String correlationId = "test-correlation-id";

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
	}

	@Test
	void testInitializeBlobWhenBlobDoesNotExist() {
		// Arrange
		Mockito.when(this.appendBlobClient.exists()).thenReturn(false);
		// Act
		this.writer = new CompanionDataWriter(this.appendBlobClient, this.correlationId, true);

		// Assert
		verify(this.appendBlobClient).exists();
		verify(this.appendBlobClient).create();
		verify(this.appendBlobClient).appendBlock(any(ByteArrayInputStream.class), anyLong());
	}

	@Test
	void testWriteWithSmallChunk() throws Exception {
		// Arrange
		Mockito.when(this.appendBlobClient.exists()).thenReturn(true);
		this.writer = new CompanionDataWriter(this.appendBlobClient, this.correlationId, true);

		List<CompanionDto> items = new ArrayList<>();
		items.add(createMockCompanionDto("1"));
		Chunk<CompanionDto> chunk = new Chunk<>(items);

		// Act
		this.writer.write(chunk);

		// Assert
		verify(this.appendBlobClient, times(2)).appendBlock(any(ByteArrayInputStream.class), anyLong());
	}

	@Test
	void testWriteWithMultipleItems() throws Exception {
		// Arrange
		Mockito.when(this.appendBlobClient.exists()).thenReturn(true);
		this.writer = new CompanionDataWriter(this.appendBlobClient, this.correlationId, true);

		List<CompanionDto> items = new ArrayList<>();
		for (int i = 0; i < 5; i++) {
			items.add(createMockCompanionDto(String.valueOf(i)));
		}
		Chunk<CompanionDto> chunk = new Chunk<>(items);

		// Act
		this.writer.write(chunk);

		// Assert
		verify(this.appendBlobClient, times(2)).appendBlock(any(ByteArrayInputStream.class), anyLong());
	}

	@Test
	void testWriteWithLargeDataSplitIntoBatches() throws Exception {
		// Arrange
		Mockito.when(this.appendBlobClient.exists()).thenReturn(true);
		this.writer = new CompanionDataWriter(this.appendBlobClient, this.correlationId, true);

		// Create a mock ObjectMapper to return large strings
		ObjectMapper realMapper = new ObjectMapper();
		ObjectMapper spyMapper = spy(realMapper);

		// Use reflection to replace the objectMapper in the this.writer
		java.lang.reflect.Field field = CompanionDataWriter.class.getDeclaredField("objectMapper");
		field.setAccessible(true);
		field.set(this.writer, spyMapper);

		// Generate a large string that will exceed batch size for a single item
		String largeJson = "{\"id\":\"large\",\"data\":\"" + "X".repeat(2 * 1024 * 1024) + "\"}";

		// Create test data
		List<CompanionDto> items = new ArrayList<>();
		CompanionDto dto1 = createMockCompanionDto("1");
		CompanionDto dto2 = createMockCompanionDto("2");
		items.add(dto1);
		items.add(dto2);

		// Make first item return large JSON
		Mockito.doReturn(largeJson).when(spyMapper).writeValueAsString(dto1);

		Chunk<CompanionDto> chunk = new Chunk<>(items);

		// Act
		this.writer.write(chunk);

		// Assert - should be called twice because items split into 2 batches
		verify(this.appendBlobClient, times(2)).appendBlock(any(ByteArrayInputStream.class), anyLong());
	}

	@Test
	void testWriteExceedsMaxOperations() throws Exception {
		// Arrange
		Mockito.when(this.appendBlobClient.exists()).thenReturn(true);
		this.writer = new CompanionDataWriter(this.appendBlobClient, this.correlationId, true);

		// Set operationCount to max using reflection
		java.lang.reflect.Field field = CompanionDataWriter.class.getDeclaredField("operationCount");
		field.setAccessible(true);
		field.set(this.writer, 50000);

		List<CompanionDto> items = new ArrayList<>();
		items.add(createMockCompanionDto("1"));
		Chunk<CompanionDto> chunk = new Chunk<>(items);

		// Act & Assert
		Assertions.assertThatException()
			.isThrownBy(() -> this.writer.write(chunk))
			.isInstanceOf(CompanionFileCreationException.class);

	}

	@Test
	void testFinalizeBlob() {
		// Arrange
		Mockito.when(this.appendBlobClient.exists()).thenReturn(true);

		this.writer = new CompanionDataWriter(this.appendBlobClient, this.correlationId, true);

		// Act
		this.writer.finalizeBlob();

		// Assert
		verify(this.appendBlobClient, times(2)).appendBlock(any(ByteArrayInputStream.class), anyLong());
	}

	@Test
	void testWriteHandlesFirstWriteFlag() throws Exception {
		// Arrange
		Mockito.when(this.appendBlobClient.exists()).thenReturn(true);
		this.writer = new CompanionDataWriter(this.appendBlobClient, this.correlationId, true);

		List<CompanionDto> items = new ArrayList<>();
		items.add(createMockCompanionDto("1"));
		Chunk<CompanionDto> chunk = new Chunk<>(items);

		// Act - first write
		this.writer.write(chunk);

		// Capture the argument
		ArgumentCaptor<ByteArrayInputStream> inputStreamCaptor = ArgumentCaptor.forClass(ByteArrayInputStream.class);
		verify(this.appendBlobClient, times(2)).appendBlock(inputStreamCaptor.capture(), anyLong());
		String firstWriteContent = new String(inputStreamCaptor.getValue().readAllBytes(), StandardCharsets.UTF_8);

		// Should not start with a comma since it's the first write
		Assertions.assertThat(firstWriteContent).doesNotStartWith(",");

		// Second write should include comma
		Mockito.reset(this.appendBlobClient);
		this.writer.write(chunk);

		inputStreamCaptor = ArgumentCaptor.forClass(ByteArrayInputStream.class);
		verify(this.appendBlobClient).appendBlock(inputStreamCaptor.capture(), anyLong());
		String secondWriteContent = new String(inputStreamCaptor.getValue().readAllBytes(), StandardCharsets.UTF_8);

		// Should start with a comma since it's the second write
		Assertions.assertThat(secondWriteContent).startsWith(",");
	}

	private CompanionDto createMockCompanionDto(String networkPaymentId) {
		CompanionDto dto = new CompanionDto();
		dto.setNetworkPaymentID(networkPaymentId);
		return dto;
	}

}
