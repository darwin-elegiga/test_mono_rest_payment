package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.dto.document.TemplateMappedValues;
import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Getter
@RequiredArgsConstructor
public class HtmlGenerationResultWrapper {

	private final String htmlContent;

	private final TemplateMappedValues templateMappedValues;

}
