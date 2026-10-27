package com.uhg.optum.financials.payments.payer.job;

import com.uhg.optum.financials.payments.payer.service.PostedFileCreationService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Configuration;

import java.io.IOException;

@Slf4j
@Configuration
public class PostedFileCreationJob {

	private final PostedFileCreationService postedFileCreationService;

	@Autowired
	PostedFileCreationJob(PostedFileCreationService postedFileCreationService) {
		this.postedFileCreationService = postedFileCreationService;
	}

	public void postedFileCreationJob() throws IOException {
		log.info("{} is Created.", this.postedFileCreationService.createFile());
	}

}
