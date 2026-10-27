package com.uhg.optum.financials.payments.payer.dto.batch;

import com.fasterxml.jackson.annotation.JsonFormat;
import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotEmpty;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDate;

@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class BatchUpdateTimeRangeRequestDto {

	@Getter
	@Setter
	@NotEmpty(message = "Status is required")
	@Schema(description = "Status to update", example = "P", allowableValues = { "P", "I", "R", "C", "F", "X" })
	private String status;

	@Getter
	@Setter
	@JsonFormat(pattern = "yyyy-MM-dd")
	@Schema(description = "Start date for date range filter", example = "2023-01-01")
	private LocalDate startDate;

	@Getter
	@Setter
	@JsonFormat(pattern = "yyyy-MM-dd")
	@Schema(description = "End date for date range filter", example = "2023-01-31")
	private LocalDate endDate;

}
