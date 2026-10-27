package com.uhg.optum.financials.payments.payer.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class CardBalanceResponseDto {

	@NotBlank
	private String cguid;

	@NotNull
	private CardBalanceResponseDto.Money availableBalance;

	@NotNull
	private CardBalanceResponseDto.Money currentBalance;

	@NotNull
	private String timestamp;

	@Data
	public static class Money {

		@NotBlank
		private String currency;

		@NotNull
		private Long value;

	}

}
