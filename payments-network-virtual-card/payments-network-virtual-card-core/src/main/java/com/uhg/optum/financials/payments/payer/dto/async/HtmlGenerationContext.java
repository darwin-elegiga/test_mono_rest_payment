package com.uhg.optum.financials.payments.payer.dto.async;

import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;
import java.util.Map;
import java.util.Set;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class HtmlGenerationContext {

	private Long batchId;

	private PaymentRequest paymentRequest;

	private String correlationId;

	private LocalDateTime batchCreatedAt;

	private Set<String> addedLogoUrls;

	private Map<String, String> addedOrgLogos;

}
