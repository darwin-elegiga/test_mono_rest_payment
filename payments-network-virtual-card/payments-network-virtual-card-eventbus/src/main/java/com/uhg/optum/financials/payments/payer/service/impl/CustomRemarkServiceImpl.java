package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.model.DocumentCustomRemarks;
import com.uhg.optum.financials.payments.payer.repository.DocumentCustomRemarksRepository;
import com.uhg.optum.financials.payments.payer.service.CustomRemarkService;
import com.uhg.optum.financials.payments.remittance.events.CustomRemarkUpdated;
import com.uhg.optum.financials.payments.remittance.events.DefaultCustomRemarks;
import com.uhg.optum.financials.payments.remittance.events.models.CustomRemark;
import com.uhg.optum.financials.payments.remittance.events.models.DefaultCustomRemark;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;

@Service
@Slf4j
public class CustomRemarkServiceImpl implements CustomRemarkService {

	private final DocumentCustomRemarksRepository documentCustomRemarksRepository;

	public CustomRemarkServiceImpl(DocumentCustomRemarksRepository documentCustomRemarksRepository) {
		this.documentCustomRemarksRepository = documentCustomRemarksRepository;
	}

	@Override
	public void saveCustomRemark(CustomRemarkUpdated customRemarkUpdated) {
		DocumentCustomRemarks documentCustomRemarks = this.documentCustomRemarksRepository
			.findByOrgShortCodeAndAccountShortCodeAndSubAccountShortCode(customRemarkUpdated.getOrgShortCode(),
					customRemarkUpdated.getAccountShortCode(), customRemarkUpdated.getSubAccountShortCode());
		if (documentCustomRemarks == null) {
			documentCustomRemarks = new DocumentCustomRemarks();
			documentCustomRemarks.setOrgShortCode(customRemarkUpdated.getOrgShortCode());
			documentCustomRemarks.setAccountShortCode(customRemarkUpdated.getAccountShortCode());
			documentCustomRemarks.setSubAccountShortCode(customRemarkUpdated.getSubAccountShortCode());
			documentCustomRemarks.setCreatedAt(LocalDateTime.now());
		}
		CustomRemark[] customRemarks = customRemarkUpdated.getCustomRemarks();
		documentCustomRemarks.setCustomRemarks1((customRemarks.length > 0) ? customRemarks[0].getRemark() : null);
		documentCustomRemarks.setCustomRemarks2((customRemarks.length > 1) ? customRemarks[1].getRemark() : null);

		this.documentCustomRemarksRepository.save(documentCustomRemarks);
	}

	@Override
	public void saveDefaultCustomRemarks(DefaultCustomRemarks eventPayload) {
		DocumentCustomRemarks documentCustomRemarks = this.documentCustomRemarksRepository
			.findByOrgShortCodeIsNullAndAccountShortCodeIsNullAndSubAccountShortCodeIsNull();
		if (documentCustomRemarks == null) {
			documentCustomRemarks = new DocumentCustomRemarks();
			documentCustomRemarks.setOrgShortCode(null);
			documentCustomRemarks.setAccountShortCode(null);
			documentCustomRemarks.setSubAccountShortCode(null);
			documentCustomRemarks.setCreatedAt(LocalDateTime.now());
		}
		DefaultCustomRemark[] defaultCustomRemarks = eventPayload.getCustomRemarks();
		for (DefaultCustomRemark defaultCustomRemark : defaultCustomRemarks) {
			CustomRemark[] customRemarks = defaultCustomRemark.getRemarks();
			documentCustomRemarks.setCustomRemarks1((customRemarks.length > 0) ? customRemarks[0].getRemark() : null);
			documentCustomRemarks.setCustomRemarks2((customRemarks.length > 1) ? customRemarks[1].getRemark() : null);
		}

		this.documentCustomRemarksRepository.save(documentCustomRemarks);
	}

}
