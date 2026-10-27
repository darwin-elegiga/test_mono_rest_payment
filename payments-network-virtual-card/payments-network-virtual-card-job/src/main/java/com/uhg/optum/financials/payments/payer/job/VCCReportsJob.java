package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.service.PostedFileCreationService;
import com.uhg.optum.financials.payments.payer.service.impl.AuthorizedFileCreationServiceImpl;
import com.uhg.optum.financials.payments.payer.service.impl.VCCNonFinancialFileServiceImpl;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.io.IOException;

@Service
@Slf4j
public class VCCReportsJob {

	private final VCCNonFinancialFileServiceImpl vccNonFinancialFile;

	private final PostedFileCreationService postedFileCreationService;

	private final AuthorizedFileCreationServiceImpl authorizedFileCreationService;

	public VCCReportsJob(VCCNonFinancialFileServiceImpl vccNonFinancialFile,
			PostedFileCreationService postedFileCreationService,
			AuthorizedFileCreationServiceImpl authorizedFileCreationService) {
		this.vccNonFinancialFile = vccNonFinancialFile;
		this.postedFileCreationService = postedFileCreationService;
		this.authorizedFileCreationService = authorizedFileCreationService;
	}

	public void generateVCCNonFinancialFile() throws IOException {
		log.info("{} Non Financial file is created.", this.vccNonFinancialFile.createFile());
	}

	public void generatePostedFile() throws IOException {
		log.info("{} Posted File is created.", this.postedFileCreationService.createFile());
	}

	public void generateAuthorizedFile() throws IOException {
		log.info("{} Authorized File is created.", this.authorizedFileCreationService.createFile());
	}

}
