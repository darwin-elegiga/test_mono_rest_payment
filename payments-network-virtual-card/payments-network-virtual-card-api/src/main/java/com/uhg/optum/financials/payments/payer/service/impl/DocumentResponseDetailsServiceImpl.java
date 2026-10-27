package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.DocumentResponseDetails;
import com.uhg.optum.financials.payments.payer.repository.DocumentResponseDetailsRepository;
import com.uhg.optum.financials.payments.payer.service.DocumentResponseDetailsService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class DocumentResponseDetailsServiceImpl implements DocumentResponseDetailsService {

	@Autowired
	private DocumentResponseDetailsRepository documentResponseDetailsRepository;

	@Override
	public List<DocumentResponseDetails> getDocumentResponseDetails(String networkPaymentId) {

		List<DocumentResponseDetails> documentResponseDetailsList = this.documentResponseDetailsRepository
			.findByNetworkPaymentId(networkPaymentId);
		if (documentResponseDetailsList == null || documentResponseDetailsList.isEmpty()) {
			throw new NotFoundException("No DocumentResponseDetails found for Network Payment ID: " + networkPaymentId);
		}

		return documentResponseDetailsList;
	}

}
