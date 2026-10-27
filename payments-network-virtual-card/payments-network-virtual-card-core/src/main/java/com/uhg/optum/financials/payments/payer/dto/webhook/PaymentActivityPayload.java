package com.uhg.optum.financials.payments.payer.dto.webhook;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.Getter;
import lombok.Setter;

public class PaymentActivityPayload {

	/**
	 * The amount associated with the event.
	 * <p>
	 * Example - 50.00
	 */
	@Getter
	@Setter
	@Schema(description = "Amount", example = "50.00")
	private Double redeemedAmount;

	/**
	 * The Action associated with the event.
	 * <p>
	 * Example -Settled
	 */
	@Getter
	@Setter
	@Schema(description = "Action", example = "Settled")
	private String action;

	/**
	 * The description of the action associated with the event.
	 * <p>
	 * Example - The payment has been settled
	 */
	@Getter
	@Setter
	@Schema(description = "The description of the action associated with the event.",
			example = "The payment has been settled")
	private String actionDescription;

	/**
	 * The reason for the void/unload.
	 * <p>
	 * Valid reason codes: UserRequestSendback, Escheatment, PaymentNetworkExhausted.
	 */
	@Getter
	@Setter
	@Schema(description = "Reason Code", example = "Escheatment")
	private String reasonCode;

	@Getter
	@Setter
	@Schema(description = "New Available Balance", example = "1000.00")
	private Double newAvailableBalance;

}
