package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.MappedSuperclass;
import lombok.Getter;
import lombok.Setter;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDateTime;

@MappedSuperclass
@Getter
@Setter
public class BaseEntity {

	/**
	 * nullable persistent field.
	 */
	@Column(length = 20)
	private String createdBy;

	/**
	 * nullable persistent field.
	 */
	@CreationTimestamp
	private LocalDateTime createdOn;

	/**
	 * persistent field.
	 */
	@Column(length = 20)
	private String updatedBy;

	/**
	 * nullable persistent field.
	 */
	@UpdateTimestamp
	private LocalDateTime updatedOn;

}
