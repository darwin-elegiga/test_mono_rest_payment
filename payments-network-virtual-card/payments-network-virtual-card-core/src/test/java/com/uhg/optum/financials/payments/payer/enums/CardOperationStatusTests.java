package com.uhg.optum.financials.payments.payer.enums;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.assertj.core.api.Assertions;

@DisplayName("CardOperationStatus Enum Tests")
class CardOperationStatusTests {

	@Test
	@DisplayName("should contain all expected enum values and descriptions")
	void shouldContainAllEnumValuesAndDescriptions() {
		Assertions.assertThat(CardOperationStatus.valueOf("REQUEST_RECEIVED"))
			.isEqualTo(CardOperationStatus.REQUEST_RECEIVED);
		Assertions.assertThat(CardOperationStatus.valueOf("REQUEST_INITIATED"))
			.isEqualTo(CardOperationStatus.REQUEST_INITIATED);
		Assertions.assertThat(CardOperationStatus.valueOf("REQUEST_ACCEPTED"))
			.isEqualTo(CardOperationStatus.REQUEST_ACCEPTED);
		Assertions.assertThat(CardOperationStatus.valueOf("REQUEST_REJECTED"))
			.isEqualTo(CardOperationStatus.REQUEST_REJECTED);
		Assertions.assertThat(CardOperationStatus.valueOf("CARD_ISSUED")).isEqualTo(CardOperationStatus.CARD_ISSUED);
		Assertions.assertThat(CardOperationStatus.valueOf("CARD_ISSUE_FAILED"))
			.isEqualTo(CardOperationStatus.CARD_ISSUE_FAILED);
		Assertions.assertThat(CardOperationStatus.valueOf("CARD_CANCELLED"))
			.isEqualTo(CardOperationStatus.CARD_CANCELLED);
		Assertions.assertThat(CardOperationStatus.valueOf("CARD_CANCELLATION_FAILED"))
			.isEqualTo(CardOperationStatus.CARD_CANCELLATION_FAILED);
		Assertions.assertThat(CardOperationStatus.valueOf("EXCEPTION_OCCURRED"))
			.isEqualTo(CardOperationStatus.EXCEPTION_OCCURRED);
		Assertions.assertThat(CardOperationStatus.valueOf("REQUEST_PROCESSING_FAILED"))
			.isEqualTo(CardOperationStatus.REQUEST_PROCESSING_FAILED);
		Assertions.assertThat(CardOperationStatus.valueOf("CALLBACK_PROCESSING_FAILED"))
			.isEqualTo(CardOperationStatus.CALLBACK_PROCESSING_FAILED);
		Assertions.assertThat(CardOperationStatus.valueOf("ERROR_OCCURRED"))
			.isEqualTo(CardOperationStatus.ERROR_OCCURRED);
		Assertions.assertThat(CardOperationStatus.valueOf("AUTH_REQUEST_ACCEPTED"))
			.isEqualTo(CardOperationStatus.AUTH_REQUEST_ACCEPTED);
		Assertions.assertThat(CardOperationStatus.valueOf("AUTH_REQUEST_REJECTED"))
			.isEqualTo(CardOperationStatus.AUTH_REQUEST_REJECTED);
		Assertions.assertThat(CardOperationStatus.valueOf("AUTH_RESET")).isEqualTo(CardOperationStatus.AUTH_RESET);
		Assertions.assertThat(CardOperationStatus.valueOf("AUTH_RESET_FAILED"))
			.isEqualTo(CardOperationStatus.AUTH_RESET_FAILED);
	}

	@Test
	@DisplayName("should return correct description for each enum value")
	void shouldReturnCorrectDescriptionForEachEnum() {
		Assertions.assertThat(CardOperationStatus.REQUEST_RECEIVED.getDescription())
			.isEqualTo("Request Received from Issuing");
		Assertions.assertThat(CardOperationStatus.REQUEST_INITIATED.getDescription()).isEqualTo("Request sent to Zeta");
		Assertions.assertThat(CardOperationStatus.REQUEST_ACCEPTED.getDescription())
			.isEqualTo("Zeta responded positively");
		Assertions.assertThat(CardOperationStatus.REQUEST_REJECTED.getDescription())
			.isEqualTo("Zeta rejected the request");
		Assertions.assertThat(CardOperationStatus.CARD_ISSUED.getDescription())
			.isEqualTo("Card successfully created via Webhook");
		Assertions.assertThat(CardOperationStatus.CARD_ISSUE_FAILED.getDescription())
			.isEqualTo("Card creation failed via Webhook");
		Assertions.assertThat(CardOperationStatus.CARD_CANCELLED.getDescription())
			.isEqualTo("Card successfully cancelled via Webhook");
		Assertions.assertThat(CardOperationStatus.CARD_CANCELLATION_FAILED.getDescription())
			.isEqualTo("Card cancellation failed via Webhook");
		Assertions.assertThat(CardOperationStatus.EXCEPTION_OCCURRED.getDescription())
			.isEqualTo("Exception occurred during processing");
		Assertions.assertThat(CardOperationStatus.REQUEST_PROCESSING_FAILED.getDescription())
			.isEqualTo("Failure occurred while processing the request internally");
		Assertions.assertThat(CardOperationStatus.CALLBACK_PROCESSING_FAILED.getDescription())
			.isEqualTo("Failure occurred while processing the webhook callback internally");
		Assertions.assertThat(CardOperationStatus.ERROR_OCCURRED.getDescription())
			.isEqualTo("Error occurred during processing");
		Assertions.assertThat(CardOperationStatus.AUTH_REQUEST_ACCEPTED.getDescription())
			.isEqualTo("Authorization request accepted");
		Assertions.assertThat(CardOperationStatus.AUTH_REQUEST_REJECTED.getDescription())
			.isEqualTo("Authorization request rejected");
		Assertions.assertThat(CardOperationStatus.AUTH_RESET.getDescription())
			.isEqualTo("Authorization reset successfully");
		Assertions.assertThat(CardOperationStatus.AUTH_RESET_FAILED.getDescription())
			.isEqualTo("Authorization reset failed");
	}

	@Test
	@DisplayName("should return all values in values() array")
	void shouldReturnAllValuesInArray() {
		CardOperationStatus[] values = CardOperationStatus.values();
		Assertions.assertThat(values)
			.contains(CardOperationStatus.REQUEST_RECEIVED, CardOperationStatus.REQUEST_INITIATED,
					CardOperationStatus.REQUEST_ACCEPTED, CardOperationStatus.REQUEST_REJECTED,
					CardOperationStatus.CARD_ISSUED, CardOperationStatus.CARD_ISSUE_FAILED,
					CardOperationStatus.CARD_CANCELLED, CardOperationStatus.CARD_CANCELLATION_FAILED,
					CardOperationStatus.EXCEPTION_OCCURRED, CardOperationStatus.REQUEST_PROCESSING_FAILED,
					CardOperationStatus.CALLBACK_PROCESSING_FAILED, CardOperationStatus.ERROR_OCCURRED,
					CardOperationStatus.AUTH_REQUEST_ACCEPTED, CardOperationStatus.AUTH_REQUEST_REJECTED,
					CardOperationStatus.AUTH_RESET, CardOperationStatus.AUTH_RESET_FAILED);
	}

	@Test
	@DisplayName("should return correct name for each enum value")
	void shouldReturnCorrectNameForEachEnum() {
		Assertions.assertThat(this.getEnumNames())
			.contains("REQUEST_RECEIVED", "REQUEST_INITIATED", "REQUEST_ACCEPTED", "REQUEST_REJECTED", "CARD_ISSUED",
					"CARD_ISSUE_FAILED", "CARD_CANCELLED", "CARD_CANCELLATION_FAILED", "EXCEPTION_OCCURRED",
					"REQUEST_PROCESSING_FAILED", "CALLBACK_PROCESSING_FAILED", "ERROR_OCCURRED",
					"AUTH_REQUEST_ACCEPTED", "AUTH_REQUEST_REJECTED", "AUTH_RESET", "AUTH_RESET_FAILED");
	}

	private String[] getEnumNames() {
		CardOperationStatus[] values = CardOperationStatus.values();
		String[] names = new String[values.length];
		for (int i = 0; i < values.length; i++) {
			names[i] = values[i].name();
		}
		return names;
	}

}
