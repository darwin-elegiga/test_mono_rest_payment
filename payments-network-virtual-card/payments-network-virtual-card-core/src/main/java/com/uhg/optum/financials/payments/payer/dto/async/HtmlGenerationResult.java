package com.uhg.optum.financials.payments.payer.dto.async;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.util.HashMap;
import java.util.Map;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class HtmlGenerationResult {

	private String paymentId;

	private String htmlFileName;

	private String htmlContent;

	private String supplementalRecord;

	private long generationTimeMs;

	private boolean success;

	private String errorMessage;

	private Map<String, byte[]> logoData = new HashMap<>();

	private String orgLogoHash;

	private String orgLogoSource;

	public HtmlGenerationResult(String paymentId, String htmlFileName) {
		this.paymentId = paymentId;
		this.htmlFileName = htmlFileName;
		this.logoData = new HashMap<>();
	}

	@Override
	public String toString() {
		return "HtmlGenerationResult{" + "paymentId='" + this.paymentId + '\'' + ", htmlFileName='" + this.htmlFileName
				+ '\'' + ", generationTimeMs=" + this.generationTimeMs + ", success=" + this.success
				+ ", errorMessage='" + this.errorMessage + '\'' + ", logosCount=" + this.logoData.size() + '}';
	}

}
