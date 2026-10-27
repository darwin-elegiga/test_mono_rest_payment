package com.uhg.optum.financials.payments.payer.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.uhg.optum.financials.payments.payer.dto.stp.StpConsumerDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpRequestHeaderDto;
import com.uhg.optum.financials.payments.payer.dto.stp.StpVirtualCardRequestDto;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import io.swagger.v3.oas.annotations.Operation;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import java.util.Map;

@ExtendWith(MockitoExtension.class)
class StpRemittanceMockControllerTests {

	@Mock
	private LogUtility logUtility;

	private StpRemittanceMockController controller;

	@BeforeEach
	void setUp() {
		this.controller = new StpRemittanceMockController(this.logUtility, new ObjectMapper());
	}

	@Test
	void receiveRemittanceNoAuthStpProvider_operationDescriptionMatchesAcceptedBehavior() throws NoSuchMethodException {
		Operation operation = StpRemittanceMockController.class
			.getMethod("receiveRemittanceNoAuthStpProvider", StpVirtualCardRequestDto.class)
			.getAnnotation(Operation.class);

		Assertions.assertThat(operation).isNotNull();
		Assertions.assertThat(operation.description())
			.isEqualTo("No-auth endpoint for STP Provider. Both C2B and B2B return success.");
	}

	@Test
	void receiveRemittanceNoAuthStpProvider_acceptsB2bPayload() {
		ResponseEntity<Map<String, Object>> response = this.controller
			.receiveRemittanceNoAuthStpProvider(createPayload("NPID-B2B", "STP Partner", false));

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isNotNull();
		Assertions.assertThat(response.getBody().get("status")).isEqualTo("ACCEPTED");
		Assertions.assertThat(response.getBody().get("networkPaymentId")).isEqualTo("NPID-B2B");
		Assertions.assertThat(response.getBody().get("networkPartner")).isEqualTo("STP Partner");
	}

	@Test
	void receiveRemittanceNoAuthStpProvider_acceptsC2bPayload() {
		ResponseEntity<Map<String, Object>> response = this.controller
			.receiveRemittanceNoAuthStpProvider(createPayload("NPID-C2B", "STP Partner", true));

		Assertions.assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
		Assertions.assertThat(response.getBody()).isNotNull();
		Assertions.assertThat(response.getBody().get("status")).isEqualTo("ACCEPTED");
		Assertions.assertThat(response.getBody().get("networkPaymentId")).isEqualTo("NPID-C2B");
		Assertions.assertThat(response.getBody().get("networkPartner")).isEqualTo("STP Partner");
	}

	private StpVirtualCardRequestDto createPayload(String networkPaymentId, String networkPartner, boolean c2b) {
		StpRequestHeaderDto header = new StpRequestHeaderDto();
		header.setNetworkPaymentId(networkPaymentId);
		header.setNetworkPartner(networkPartner);

		StpVirtualCardRequestDto payload = new StpVirtualCardRequestDto();
		payload.setHeader(header);
		if (c2b) {
			payload.setConsumer(new StpConsumerDto());
		}
		return payload;
	}

}
