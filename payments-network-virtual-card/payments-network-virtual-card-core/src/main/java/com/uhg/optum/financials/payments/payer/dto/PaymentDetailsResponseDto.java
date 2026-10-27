package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.shared.client.dto.payment.PaymentDetailsDto;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class PaymentDetailsResponseDto extends PaymentDetailsDto {

	@Schema(description = "network payment identifier", example = "NPID123456")
	private String networkPaymentId;

}
