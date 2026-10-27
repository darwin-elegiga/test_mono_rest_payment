package com.uhg.optum.financials.payments.payer.model;

import com.uhg.optum.financials.payments.payer.dto.FileTrackingDto;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import java.util.Optional;

@Entity
@Getter
@Setter
@NoArgsConstructor
@Table(name = "file_tracking", schema = "virtual_card")
public class FileTracking {

	@Column(name = "835")
	private String _835;

	@Column(name = "companion")
	private String companion;

	@Column(name = "companion_status")
	private String companionStatus;

	@Column(name = "payment_activity_status")
	private String paymentActivityStatus;

	@Id
	@Column(name = "correlation_id")
	private String correlationId;

	public FileTrackingDto toDto(FileTrackingDto fileTrackingDto) {
		fileTrackingDto = Optional.ofNullable(fileTrackingDto).orElse(new FileTrackingDto());
		fileTrackingDto.set_835(this._835);
		fileTrackingDto.setCompanion(this.companion);
		fileTrackingDto.setCompanionStatus(this.companionStatus);
		fileTrackingDto.setPaymentActivityStatus(this.paymentActivityStatus);
		fileTrackingDto.setCorrelationId(this.correlationId);
		return fileTrackingDto;
	}

}
