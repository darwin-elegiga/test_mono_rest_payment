package com.uhg.optum.financials.payments.payer.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotBlank;
import lombok.Getter;
import lombok.Setter;
import lombok.ToString;

import java.io.Serializable;

@Getter
@Setter
@ToString
public class Remit835SplitRequestDto implements Serializable {

	@NotBlank(message = "835 File Path cannot be null or empty")
	@Schema(description = "Path to the 835 file", example = "/path/to/835/file.835")
	private String remit835FilePath;

	@NotBlank(message = "Payment Mapping File Path cannot be null or empty")
	@Schema(description = "Path to the payment mapping file", example = "/path/to/payment/mapping/file.json")
	private String paymentMappingFilePath;

}
