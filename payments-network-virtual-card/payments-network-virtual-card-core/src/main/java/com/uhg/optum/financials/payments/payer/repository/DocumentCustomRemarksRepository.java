package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.DocumentCustomRemarks;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface DocumentCustomRemarksRepository extends JpaRepository<DocumentCustomRemarks, Integer> {

	DocumentCustomRemarks findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode(String orgShortCode,
			String accountShortCode, String subAccountShortCode);

	DocumentCustomRemarks findByOrgShortCodeIsNullAndAccountShortCodeIsNullAndSubAccountShortCodeIsNull();

	List<DocumentCustomRemarks> findByCustomRemarks1NotNull();

}
