package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.BinResponse;
import com.uhg.optum.financials.payments.payer.dto.CreateBinRequest;
import com.uhg.optum.financials.payments.payer.dto.UpdateBinRequest;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.Bin;
import com.uhg.optum.financials.payments.payer.repository.BinRepository;
import com.uhg.optum.financials.payments.payer.service.BinConfigService;
import com.uhg.optum.financials.payments.payer.util.UserNameExtractUtil;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.List;

@Service
@RequiredArgsConstructor
@Transactional
public class BinConfigServiceImpl implements BinConfigService {

	private final BinRepository binRepository;

	@Override
	public List<BinResponse> getAllBins() {
		Sort sort = Sort.by(Sort.Order.asc("id"));
		return this.binRepository.findAll(sort).stream().map(this::toResponse).toList();
	}

	@Override
	public BinResponse saveBin(CreateBinRequest request) {
		if (this.binRepository.existsByFundingType(request.getFundingType())) {
			throw new BadRequestException("Funding type already exists: " + request.getFundingType());
		}

		Bin bin = new Bin();
		bin.setBin(request.getBin());
		bin.setFundingType(request.getFundingType());
		bin.setCreatedAt(LocalDateTime.now());
		bin.setCreatedBy(UserNameExtractUtil.getUserNameFromToken());
		Bin saved = this.binRepository.save(bin);
		return toResponse(saved);
	}

	@Override
	public BinResponse updateBin(Long id, UpdateBinRequest request) {
		Bin entity = this.binRepository.findById(id)
			.orElseThrow(() -> new NotFoundException("Bin not found with id: " + id));

		boolean changed = false;

		if (request.getBin() != null && !request.getBin().equals(entity.getBin())) {
			entity.setBin(request.getBin());
			changed = true;
		}

		if (request.getFundingType() != null && !request.getFundingType().equals(entity.getFundingType())) {
			if (this.binRepository.existsByFundingTypeAndIdNot(request.getFundingType(), id)) {
				throw new BadRequestException("Funding type already exists: " + request.getFundingType());
			}
			entity.setFundingType(request.getFundingType());
			changed = true;
		}
		if (!changed) {
			return toResponse(entity);
		}
		entity.setUpdatedAt(LocalDateTime.now());
		entity.setUpdatedBy(UserNameExtractUtil.getUserNameFromToken());
		Bin saved = this.binRepository.save(entity);
		return toResponse(saved);
	}

	private BinResponse toResponse(Bin entity) {
		BinResponse dto = new BinResponse();
		dto.setId(entity.getId());
		dto.setBin(entity.getBin());
		dto.setFundingType(entity.getFundingType());
		return dto;
	}

}
