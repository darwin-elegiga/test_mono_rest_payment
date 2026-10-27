package com.uhg.optum.financials.payments.payer.service;

import jakarta.servlet.http.HttpServletResponse;
import org.springframework.web.servlet.mvc.method.annotation.StreamingResponseBody;

public interface FileService {

	StreamingResponseBody downloadBlobFile(String fileNameWithPath, HttpServletResponse response);

}
