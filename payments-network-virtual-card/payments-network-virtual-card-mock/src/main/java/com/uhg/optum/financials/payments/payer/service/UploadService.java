package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.document.DocumentResponse;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;

public interface UploadService {

	DocumentResponse uploadFile(MultipartFile file) throws IOException;

}
