package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.Consolidated835FileDto;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.INFO;

@Tag(name = "Mock Remittance 835", description = "Mock endpoint for testing 835 companion file remittance processing")
@RestController
@RequestMapping("/v2/835")
@Slf4j
public class MockRemittance835Controller {

	@Value("${mock.remittance.process-c835.delay-ms:0}")
	private long delayMs;

	private final LogUtility logUtility;

	public MockRemittance835Controller(LogUtility logUtility) {
		this.logUtility = logUtility;
	}

	@Operation(summary = "Process 835 companion file",
			description = "Mock endpoint simulating remittance service processing of a consolidated 835 + companion file pair")
	@PostMapping("/process")
	public ResponseEntity<Void> process835(@RequestBody Consolidated835FileDto payload) throws InterruptedException {
		this.logUtility.logMessage("Mock remittance 835 process called for orgShortCode: " + payload.getOrgShortCode(),
				INFO, log);

		if (this.delayMs > 0) {
			this.logUtility.logMessage("Simulating delay of " + this.delayMs + "ms for remittance processing", INFO,
					log);
			Thread.sleep(this.delayMs);
		}

		this.logUtility.logMessage(
				"Mock remittance 835 processing complete for orgShortCode: " + payload.getOrgShortCode(), INFO, log);
		return ResponseEntity.ok().build();
	}

}
