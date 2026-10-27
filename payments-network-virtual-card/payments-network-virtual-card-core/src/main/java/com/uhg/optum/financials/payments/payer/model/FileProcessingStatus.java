package com.uhg.optum.financials.payments.payer.model;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.SequenceGenerator;
import jakarta.persistence.Table;
import lombok.Data;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

import java.time.LocalDateTime;

@Entity
@Data
@Table(name = "file_processing_status", schema = "virtual_card")
public class FileProcessingStatus {

	@Id
	@GeneratedValue(strategy = GenerationType.SEQUENCE, generator = "virtual_card.file_processing_status_id_seq")
	@SequenceGenerator(name = "virtual_card.file_processing_status_id_seq", allocationSize = 1)
	private Long id;

	@Column(name = "file_name", nullable = false, length = 255)
	private String fileName;

	@Column(name = "file_type", nullable = false, length = 50)
	private String fileType; // CLEAR or AUTH

	@Column(name = "status", nullable = false, length = 50)
	private String status; // SUCCESS or FAILED or PARTIAL_SUCCESS

	@Column(name = "record_count")
	private Integer recordCount;

	@Column(name = "error_message", columnDefinition = "TEXT")
	private String errorMessage;

	@Column(name = "processed_at", nullable = false)
	private LocalDateTime processedAt;

	@Column(name = "processed_by", nullable = false, length = 100)
	private String processedBy;

	@JdbcTypeCode(SqlTypes.JSON)
	@Column(name = "record_errors")
	private String recordErrors; // store JSON as String

}
