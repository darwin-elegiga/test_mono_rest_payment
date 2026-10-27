package com.uhg.optum.financials.payments.payer.dto;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import static org.assertj.core.api.BDDAssertions.then;
import java.util.UUID;

class CancelCardDtoTests {

	private CancelCardDto cancelCardDto;

	@Test
	@DisplayName("should set and get paymentId, reasonCode, and cardGuid using this keyword")
	void shouldSetAndGetFields() {
		// Arrange
		this.cancelCardDto = new CancelCardDto();
		String paymentId = "PAY123";
		String reasonCode = "FRAUD_RISK";
		UUID cardGuid = UUID.randomUUID();

		// Act
		this.cancelCardDto.setPaymentId(paymentId);
		this.cancelCardDto.setReasonCode(reasonCode);
		this.cancelCardDto.setCardGuid(cardGuid);

		// Assert
		then(this.cancelCardDto.getPaymentId()).isEqualTo(paymentId);
		then(this.cancelCardDto.getReasonCode()).isEqualTo(reasonCode);
		then(this.cancelCardDto.getCardGuid()).isEqualTo(cardGuid);
	}

	@Test
	@DisplayName("should allow all Reason enum values")
	void shouldAllowAllReasonEnumValues() {
		// Assert
		then(CancelCardDto.Reason.FRAUD_RISK.name()).isEqualTo("FRAUD_RISK");
		then(CancelCardDto.Reason.OFAC_BLOCKED.name()).isEqualTo("OFAC_BLOCKED");
		then(CancelCardDto.Reason.HOTLISTED.name()).isEqualTo("HOTLISTED");
		then(CancelCardDto.Reason.OTHER.name()).isEqualTo("OTHER");
	}

}
