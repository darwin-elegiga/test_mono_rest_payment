package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.FileProcessingStatus;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.time.LocalDateTime;
import java.util.List;

public interface FileProcessingStatusRepository extends JpaRepository<FileProcessingStatus, Long> {

	@Query("SELECT COUNT(fp) FROM FileProcessingStatus fp WHERE fp.processedAt = :date and fileType = :fileType")
	int countRecordsCreatedOn(@Param("date") LocalDateTime date, @Param("fileType") String fileType);

	List<FileProcessingStatus> findAllByProcessedAtBetween(LocalDateTime start, LocalDateTime end);

}
