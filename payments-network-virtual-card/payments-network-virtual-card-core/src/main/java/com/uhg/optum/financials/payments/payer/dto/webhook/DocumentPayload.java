package com.uhg.optum.financials.payments.payer.dto.webhook;

import io.swagger.v3.oas.annotations.media.Schema;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.URL;

@AllArgsConstructor
@NoArgsConstructor
public class DocumentPayload {

	@Getter
	@Setter
	@Schema(description = "Document url", example = URL)
	private String url;

	@Getter
	@Setter
	@Schema(description = "Document type", example = "PDF")
	private String type;

}
