package com.uhg.optum.financials.payments.payer.dto.webhook;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.Map;

import static com.uhg.optum.financials.payments.payer.constant.CommonConstants.META_DATA_EX;

@Getter
@Setter
@AllArgsConstructor
@NoArgsConstructor
public class PaymentActivityWebhookRequestPayload {

	@Getter
	@Setter
	@Schema(description = "Network Payment Id", example = "123456789")
	private String networkId;

	@Getter
	@Setter
	@Schema(description = "paymentMethod", example = "DRAFT")
	private String paymentMethod;

	@Getter
	@Setter
	@Schema(description = "Check Number", example = "1223343")
	private String checkNumber;

	@Getter
	@Setter
	@Schema(description = "Vcp Status", example = "100.00")
	private String vcpStatus;

	@Getter
	@Setter
	@Schema(description = "Re-originationDate", example = "2025-10-01")
	private String reoriginationDate;

	@Getter
	@Setter
	@Schema(description = "Settled Date", example = "2025-10-01")
	private String settledDate;

	@Getter
	@Setter
	@Schema(description = "Revised Payment Method", example = "Credit Card")
	private String revisedPaymentMethod;

	@Getter
	@Setter
	@Schema(description = "norCode", example = "NOR12345")
	private String norCode;

	@Getter
	@Setter
	@Schema(description = "StateMetadata", example = META_DATA_EX)
	private Map<String, String> stateMetadata;

	@Getter
	@Setter
	@Schema(description = "Consolidated Payment Amount", example = "1000.00")
	private Double consolidatedPaymentAmount;

}
