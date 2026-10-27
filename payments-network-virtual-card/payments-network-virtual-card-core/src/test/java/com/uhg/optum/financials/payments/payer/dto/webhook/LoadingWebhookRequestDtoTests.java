package com.uhg.optum.financials.payments.payer.dto.webhook;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import static org.assertj.core.api.BDDAssertions.then;

class LoadingWebhookRequestDtoTests {

	private LoadingWebhookRequestDto<String> loadingWebhookRequestDto;

	@Test
	@DisplayName("should set allowed type successfully")
	void shouldSetAllowedTypeSuccessfully() {
		// Arrange
		this.loadingWebhookRequestDto = new LoadingWebhookRequestDto<>();
		String allowedType = "loading.completed";

		// Act
		this.loadingWebhookRequestDto.setType(allowedType);

		// Assert
		then(this.loadingWebhookRequestDto.getType()).isEqualTo(allowedType);
	}

	@Test
	@DisplayName("should throw exception for invalid type")
	void shouldThrowExceptionForInvalidType() {
		// Arrange
		this.loadingWebhookRequestDto = new LoadingWebhookRequestDto<>();
		String invalidType = "invalid.type";

		// Act & Assert
		Throwable thrown = org.assertj.core.api.Assertions
			.catchThrowable(() -> this.loadingWebhookRequestDto.setType(invalidType));
		then(thrown).isInstanceOf(IllegalArgumentException.class)
			.hasMessageContaining("Invalid type value: " + invalidType);
	}

	@Test
	@DisplayName("should return correct toString value")
	void shouldReturnCorrectToString() {
		// Arrange
		this.loadingWebhookRequestDto = new LoadingWebhookRequestDto<>();
		String allowedType = "loading.sent";
		this.loadingWebhookRequestDto.setType(allowedType);

		// Act
		String result = this.loadingWebhookRequestDto.toString();

		// Assert
		then(result).contains("LoadingWebhookRequestDto");
		then(result).contains(allowedType);
	}

}
