package com.uhg.optum.financials.payments.payer.service;

import com.uhg.optum.financials.payments.payer.dto.BinResponse;
import com.uhg.optum.financials.payments.payer.dto.CreateBinRequest;
import com.uhg.optum.financials.payments.payer.dto.UpdateBinRequest;

import java.util.List;

public interface BinConfigService {

	List<BinResponse> getAllBins();

	BinResponse saveBin(CreateBinRequest request);

	BinResponse updateBin(Long id, UpdateBinRequest request);

}
