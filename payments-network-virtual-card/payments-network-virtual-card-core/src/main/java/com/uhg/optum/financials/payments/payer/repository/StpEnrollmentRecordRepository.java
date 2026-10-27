package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.StpEnrollmentRecord;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface StpEnrollmentRecordRepository extends JpaRepository<StpEnrollmentRecord, Long> {

	boolean existsByProviderTinAndEnrollmentType(String providerTin, String enrollmentType);

}
