package com.uhg.optum.financials.payments.payer.dto;

import com.uhg.optum.financials.shared.client.dto.payment.PaymentRequestDto;
import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ST_SE_EX;

@Getter
@Setter
public class PaymentResponseDto extends PaymentRequestDto {

	private Long id;

	@Schema(description = "status of a payment", example = "PC")
	private String paymentStatus;

	@Schema(description = "st se segment", example = ST_SE_EX)
	private String stSeSegment;

	@Schema(description = "Error message describing the most recent processing failure, if any.",
			example = "Queue transmission failed: Connection timeout after 30 seconds")
	private String errorMessage;

}
