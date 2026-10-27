package com.uhg.optum.financials.payments.payer.dto.callback;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import static org.assertj.core.api.BDDAssertions.then;
import java.time.Instant;

class ZetaWebhookDtoTests {

	private String id;

	private String specversion;

	private Instant time;

	private String status;

	private String type;

	private String data;

	private String error;

	@Test
	@DisplayName("should create ZetaWebhookDto and get all fields correctly")
	void shouldCreateAndGetAllFields() {
		// Arrange
		this.id = "webhook-123";
		this.specversion = "1.0";
		this.time = Instant.parse("2025-12-01T10:15:30.00Z");
		this.status = "SUCCESS";
		this.type = "payment.created";
		this.data = "payload-data";
		this.error = "error-message";

		// Act
		ZetaWebhookDto<String> dto = new ZetaWebhookDto<>(this.id, this.specversion, this.time, this.status, this.type,
				this.data, this.error);

		// Assert
		then(dto.id()).isEqualTo(this.id);
		then(dto.specversion()).isEqualTo(this.specversion);
		then(dto.time()).isEqualTo(this.time);
		then(dto.status()).isEqualTo(this.status);
		then(dto.type()).isEqualTo(this.type);
		then(dto.data()).isEqualTo(this.data);
		then(dto.error()).isEqualTo(this.error);
	}

	@Test
	@DisplayName("should support generic data and error types using Object")
	void shouldSupportGenericTypesWithObject() {
		// Arrange
		Object data = new Object() {
			@Override
			public boolean equals(Object o) {
				return this == o;
			}

			@Override
			public int hashCode() {
				return System.identityHashCode(this);
			}
		};
		Object error = new Object() {
			@Override
			public boolean equals(Object o) {
				return this == o;
			}

			@Override
			public int hashCode() {
				return System.identityHashCode(this);
			}
		};
		ZetaWebhookDto<Object> dto = new ZetaWebhookDto<>("id", "1.0", Instant.now(), "OK", "type", data, error);

		// Assert
		then(dto.data()).isEqualTo(data);
		then(dto.error()).isEqualTo(error);
	}

}
