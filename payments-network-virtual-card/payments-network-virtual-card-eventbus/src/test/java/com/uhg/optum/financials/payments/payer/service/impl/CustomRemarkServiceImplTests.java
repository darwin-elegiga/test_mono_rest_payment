package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.repository.DocumentCustomRemarksRepository;
import com.uhg.optum.financials.payments.remittance.events.CustomRemarkUpdated;
import com.uhg.optum.financials.payments.remittance.events.DefaultCustomRemarks;
import com.uhg.optum.financials.payments.remittance.events.models.CustomRemark;
import com.uhg.optum.financials.payments.remittance.events.models.DefaultCustomRemark;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.assertj.core.api.Assertions.assertThatCode;

@ExtendWith(MockitoExtension.class)
class CustomRemarkServiceImplTests {

	@InjectMocks
	private CustomRemarkServiceImpl customRemarkServiceImpl;

	@Mock
	DocumentCustomRemarksRepository documentCustomRemarksRepository;

	// ─── saveCustomRemark ────────────────────────────────────────────────────

	@Test
	void saveCustomRemark_withTwoRemarks_setsCustomRemarks1And2() {
		CustomRemark remark1 = new CustomRemark();
		remark1.setRemark("First remark");

		CustomRemark remark2 = new CustomRemark();
		remark2.setRemark("Second remark");

		CustomRemarkUpdated event = new CustomRemarkUpdated();
		event.setOrgShortCode("ORG01");
		event.setAccountShortCode("ACC01");
		event.setSubAccountShortCode("SUB01");
		event.setCustomRemarks(new CustomRemark[] { remark1, remark2 });

		assertThatCode(() -> this.customRemarkServiceImpl.saveCustomRemark(event)).doesNotThrowAnyException();
	}

	@Test
	void saveCustomRemark_withOneRemark_setsCustomRemarks1Only() {
		CustomRemark remark1 = new CustomRemark();
		remark1.setRemark("Only remark");

		CustomRemarkUpdated event = new CustomRemarkUpdated();
		event.setOrgShortCode("ORG01");
		event.setAccountShortCode("ACC01");
		event.setSubAccountShortCode(null);
		event.setCustomRemarks(new CustomRemark[] { remark1 });

		assertThatCode(() -> this.customRemarkServiceImpl.saveCustomRemark(event)).doesNotThrowAnyException();
	}

	@Test
	void saveCustomRemark_withEmptyRemarksArray_setsNullForBothCustomRemarks() {
		CustomRemarkUpdated event = new CustomRemarkUpdated();
		event.setOrgShortCode("ORG01");
		event.setAccountShortCode(null);
		event.setSubAccountShortCode(null);
		event.setCustomRemarks(new CustomRemark[0]);

		assertThatCode(() -> this.customRemarkServiceImpl.saveCustomRemark(event)).doesNotThrowAnyException();
	}

	@Test
	void saveCustomRemark_withNullOrgShortCode_completes() {
		CustomRemark remark = new CustomRemark();
		remark.setRemark("Remark");

		CustomRemarkUpdated event = new CustomRemarkUpdated();
		event.setOrgShortCode(null);
		event.setAccountShortCode(null);
		event.setSubAccountShortCode(null);
		event.setCustomRemarks(new CustomRemark[] { remark });

		assertThatCode(() -> this.customRemarkServiceImpl.saveCustomRemark(event)).doesNotThrowAnyException();
	}

	// ─── saveDefaultCustomRemarks ────────────────────────────────────────────

	@Test
	void saveDefaultCustomRemarks_withMultipleDefaultRemarks_iteratesAll() {
		CustomRemark r1 = new CustomRemark();
		r1.setRemark("Default remark 1");

		CustomRemark r2 = new CustomRemark();
		r2.setRemark("Default remark 2");

		DefaultCustomRemark defaultCustomRemark1 = new DefaultCustomRemark();
		defaultCustomRemark1.setModality("PAPER");
		defaultCustomRemark1.setRemarks(new CustomRemark[] { r1, r2 });

		CustomRemark r3 = new CustomRemark();
		r3.setRemark("Default remark 3");

		DefaultCustomRemark defaultCustomRemark2 = new DefaultCustomRemark();
		defaultCustomRemark2.setModality("ELECTRONIC");
		defaultCustomRemark2.setRemarks(new CustomRemark[] { r3 });

		DefaultCustomRemarks event = new DefaultCustomRemarks();
		event.setCustomRemarks(new DefaultCustomRemark[] { defaultCustomRemark1, defaultCustomRemark2 });

		assertThatCode(() -> this.customRemarkServiceImpl.saveDefaultCustomRemarks(event)).doesNotThrowAnyException();
	}

	@Test
	void saveDefaultCustomRemarks_withSingleDefaultRemarkAndTwoCustomRemarks_completes() {
		CustomRemark r1 = new CustomRemark();
		r1.setRemark("Remark A");

		CustomRemark r2 = new CustomRemark();
		r2.setRemark("Remark B");

		DefaultCustomRemark defaultCustomRemark = new DefaultCustomRemark();
		defaultCustomRemark.setModality("PAPER");
		defaultCustomRemark.setRemarks(new CustomRemark[] { r1, r2 });

		DefaultCustomRemarks event = new DefaultCustomRemarks();
		event.setCustomRemarks(new DefaultCustomRemark[] { defaultCustomRemark });

		assertThatCode(() -> this.customRemarkServiceImpl.saveDefaultCustomRemarks(event)).doesNotThrowAnyException();
	}

	@Test
	void saveDefaultCustomRemarks_withDefaultRemarkHavingEmptyRemarksArray_setsNullForBothCustomRemarks() {
		DefaultCustomRemark defaultCustomRemark = new DefaultCustomRemark();
		defaultCustomRemark.setModality("PAPER");
		defaultCustomRemark.setRemarks(new CustomRemark[0]);

		DefaultCustomRemarks event = new DefaultCustomRemarks();
		event.setCustomRemarks(new DefaultCustomRemark[] { defaultCustomRemark });

		assertThatCode(() -> this.customRemarkServiceImpl.saveDefaultCustomRemarks(event)).doesNotThrowAnyException();
	}

	@Test
	void saveDefaultCustomRemarks_withEmptyDefaultCustomRemarksArray_noIterationPerformed() {
		DefaultCustomRemarks event = new DefaultCustomRemarks();
		event.setCustomRemarks(new DefaultCustomRemark[0]);

		assertThatCode(() -> this.customRemarkServiceImpl.saveDefaultCustomRemarks(event)).doesNotThrowAnyException();
	}

}
