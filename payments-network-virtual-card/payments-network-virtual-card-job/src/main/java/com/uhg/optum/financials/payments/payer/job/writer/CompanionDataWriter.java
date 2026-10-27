package com.uhg.optum.financials.payments.payer.job.writer;

import com.azure.storage.blob.specialized.AppendBlobClient;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.CompanionDto;
import com.uhg.optum.financials.payments.payer.exception.CompanionFileCreationException;
import com.uhg.optum.financials.payments.payer.util.CompanionFileUtil;
import lombok.extern.slf4j.Slf4j;
import org.springframework.batch.infrastructure.item.Chunk;
import org.springframework.batch.infrastructure.item.ItemWriter;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

@Slf4j
public class CompanionDataWriter implements ItemWriter<CompanionDto> {

	private static final int MAX_WRITE_SIZE_BYTES = 4 * 1024 * 1024; // 4MB

	private static final int MAX_OPERATIONS = 50000;

	private final ObjectMapper objectMapper;

	private int operationCount = 0;

	private boolean isFirstWrite = true;

	private final String correlationId;

	private final AppendBlobClient appendBlobClient;

	public CompanionDataWriter(AppendBlobClient appendBlobClient, String correlationId, boolean initializeBlob) {
		this.objectMapper = new ObjectMapper();
		this.appendBlobClient = appendBlobClient;
		this.correlationId = correlationId;

		if (initializeBlob) {
			initializeBlob();
		}
	}

	private void initializeBlob() {
		try {
			if (Boolean.TRUE.equals(this.appendBlobClient.exists())) {
				this.appendBlobClient.delete();
			}

			this.appendBlobClient.create();

			// Initialize JSON array
			StringBuilder jsonStart = new StringBuilder();
			jsonStart.append("{\"correlationIDs\":[");
			jsonStart.append("{\n");
			jsonStart.append("\"correlationID\": \"").append(this.correlationId).append("\",\n");
			jsonStart.append("\"networkPayments\": [\n");
			this.appendBlobClient.appendBlock(
					new ByteArrayInputStream(jsonStart.toString().getBytes(StandardCharsets.UTF_8)),
					jsonStart.length());
			this.operationCount++;

		}
		catch (Exception ex) {
			log.error("Failed to initialize blob", ex);
			throw new CompanionFileCreationException("Failed to initialize blob" + ex.getMessage());
		}
	}

	@Override
	public void write(Chunk<? extends CompanionDto> items) throws Exception {
		if (this.operationCount >= MAX_OPERATIONS) {
			throw new CompanionFileCreationException("Maximum number of blob operations reached (50K limit)");
		}

		List<CompanionDto> itemsList = new ArrayList<>(items.getItems());

		// Break the list into smaller chunks if necessary to respect the 4MB limit
		List<List<CompanionDto>> batches = splitIntoBatches(itemsList);

		for (List<CompanionDto> batch : batches) {
			StringBuilder jsonBatch = new StringBuilder();

			for (int i = 0; i < batch.size(); i++) {
				// Remove the amount field from the network payment companion line string
				String jsonItem = CompanionFileUtil.getCompanionLineWithOutAmount(batch.get(i));

				// Add comma prefix if not the first item in the file
				if (!this.isFirstWrite || i > 0) {
					jsonBatch.append(",\n");
				}

				jsonBatch.append("        ").append(jsonItem);
			}

			// Write the batch to the blob
			String batchContent = jsonBatch.toString();
			this.appendBlobClient.appendBlock(new ByteArrayInputStream(batchContent.getBytes(StandardCharsets.UTF_8)),
					batchContent.length());
			this.operationCount++;
			this.isFirstWrite = false;

			log.debug("Wrote batch of {} items to blob. Operation count: {}", batch.size(), this.operationCount);
		}
	}

	public void finalizeBlob() {
		// Close the JSON array
		String jsonEnd = "]\n}\n]\n}";
		this.appendBlobClient.appendBlock(new ByteArrayInputStream(jsonEnd.getBytes(StandardCharsets.UTF_8)),
				jsonEnd.length());
	}

	private List<List<CompanionDto>> splitIntoBatches(List<CompanionDto> items) throws IOException {
		List<List<CompanionDto>> batches = new ArrayList<>();
		List<CompanionDto> currentBatch = new ArrayList<>();
		int currentBatchSize = 0;

		for (CompanionDto item : items) {
			String jsonItem = this.objectMapper.writeValueAsString(item);
			int itemSize = jsonItem.getBytes(StandardCharsets.UTF_8).length + 10; // +10

			if (currentBatch.isEmpty() || currentBatchSize + itemSize < MAX_WRITE_SIZE_BYTES) {
				currentBatch.add(item);
				currentBatchSize += itemSize;
			}
			else {
				batches.add(currentBatch);
				currentBatch = new ArrayList<>();
				currentBatch.add(item);
				currentBatchSize = itemSize;
			}
		}

		if (!currentBatch.isEmpty()) {
			batches.add(currentBatch);
		}

		return batches;
	}

}
