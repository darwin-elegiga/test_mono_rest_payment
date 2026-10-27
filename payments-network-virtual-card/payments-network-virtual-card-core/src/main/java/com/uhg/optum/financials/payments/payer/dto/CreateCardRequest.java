package com.uhg.optum.financials.payments.payer.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;
import lombok.ToString;
import com.uhg.optum.financials.payments.payer.validation.FreeTextListValid;

import java.util.List;
import java.util.UUID;

@Getter
@Setter
@Builder
@ToString
public class CreateCardRequest {

	@NotNull(message = "requestId cannot be null.")
	private UUID requestId;

	@NotNull
	private Long bin;

	@NotNull
	@Min(1)
	@Max(9999999999L)
	private Long amount;

	private Boolean singleSpend;

	@Valid
	private Expiry expiry;

	@Valid
	private CardHolderDto cardHolder;

	@FreeTextListValid
	private List<String> spendRules;

	private boolean autoActivateCard;

}
