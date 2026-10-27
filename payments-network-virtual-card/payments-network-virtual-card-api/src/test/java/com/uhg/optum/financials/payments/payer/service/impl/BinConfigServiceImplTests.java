package com.uhg.optum.financials.payments.payer.service.impl;

import com.uhg.optum.financials.payments.payer.dto.BinResponse;
import com.uhg.optum.financials.payments.payer.dto.CreateBinRequest;
import com.uhg.optum.financials.payments.payer.dto.UpdateBinRequest;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.model.Bin;
import com.uhg.optum.financials.payments.payer.repository.BinRepository;
import org.assertj.core.api.Assertions;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Captor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.domain.Sort;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

import static org.mockito.BDDMockito.any;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.BDDMockito.willAnswer;
import static org.mockito.BDDMockito.never;

@ExtendWith(MockitoExtension.class)
class BinConfigServiceImplTests {

	@Mock
	private BinRepository binRepository;

	@InjectMocks
	private BinConfigServiceImpl service;

	@Captor
	private ArgumentCaptor<Bin> binCaptor;

	private Bin binEntity(Long id, Long bin, String fundingType) {
		Bin e = new Bin();
		e.setId(id);
		e.setBin(bin);
		e.setFundingType(fundingType);
		e.setCreatedAt(LocalDateTime.now().minusDays(1));
		e.setUpdatedAt(LocalDateTime.now().minusHours(1));
		e.setCreatedBy("test");
		e.setUpdatedBy("test");
		return e;
	}

	private CreateBinRequest createReq(Long bin, String fundingType) {
		CreateBinRequest r = new CreateBinRequest();
		r.setBin(bin);
		r.setFundingType(fundingType);
		return r;
	}

	private UpdateBinRequest updateReq(Long bin, String fundingType) {
		UpdateBinRequest r = new UpdateBinRequest();
		r.setBin(bin);
		r.setFundingType(fundingType);
		return r;
	}

	@Test
	void getAllBins_returnsMappedList_sortedByIdAsc() {
		Bin e1 = binEntity(1L, 111111L, "PRE_FUNDED_NEXT_DAY");
		Bin e2 = binEntity(2L, 546810L, "POST_FUNDED");

		given(this.binRepository.findAll(any(Sort.class))).willReturn(List.of(e1, e2));

		List<BinResponse> result = this.service.getAllBins();

		Assertions.assertThat(result).isNotNull().hasSize(2);
		Assertions.assertThat(result.get(0).getId()).isEqualTo(1L);
		Assertions.assertThat(result.get(0).getBin()).isEqualTo(111111L);
		Assertions.assertThat(result.get(0).getFundingType()).isEqualTo("PRE_FUNDED_NEXT_DAY");

		Assertions.assertThat(result.get(1).getId()).isEqualTo(2L);
		Assertions.assertThat(result.get(1).getBin()).isEqualTo(546810L);
		Assertions.assertThat(result.get(1).getFundingType()).isEqualTo("POST_FUNDED");

		ArgumentCaptor<Sort> sortCaptor = ArgumentCaptor.forClass(Sort.class);
		then(this.binRepository).should().findAll(sortCaptor.capture());
		Sort sortUsed = sortCaptor.getValue();
		Assertions.assertThat(sortUsed).isEqualTo(Sort.by(Sort.Order.asc("id")));
	}

	@Test
	void saveBin_succeeds_whenFundingTypeUnique_setsAuditFields_andMapsResponse() {
		CreateBinRequest req = createReq(548130L, "POST_FUNDED");

		given(this.binRepository.existsByFundingType("POST_FUNDED")).willReturn(false);

		willAnswer((invocation) -> {
			Bin arg = invocation.getArgument(0);
			arg.setId(10L);
			return arg;
		}).given(this.binRepository).save(any(Bin.class));

		BinResponse resp = this.service.saveBin(req);

		Assertions.assertThat(resp).isNotNull();
		Assertions.assertThat(resp.getId()).isEqualTo(10L);
		Assertions.assertThat(resp.getBin()).isEqualTo(548130L);
		Assertions.assertThat(resp.getFundingType()).isEqualTo("POST_FUNDED");

		then(this.binRepository).should().save(this.binCaptor.capture());
		Bin saved = this.binCaptor.getValue();
		Assertions.assertThat(saved.getBin()).isEqualTo(548130L);
		Assertions.assertThat(saved.getFundingType()).isEqualTo("POST_FUNDED");
		Assertions.assertThat(saved.getCreatedAt()).isNotNull();

		then(this.binRepository).should().existsByFundingType("POST_FUNDED");
	}

	@Test
	void saveBin_throwsBadRequest_whenFundingTypeAlreadyExists() {
		CreateBinRequest req = createReq(548130L, "POST_FUNDED");
		given(this.binRepository.existsByFundingType("POST_FUNDED")).willReturn(true);

		Assertions.assertThatThrownBy(() -> this.service.saveBin(req))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Funding type already exists: POST_FUNDED");

		then(this.binRepository).should().existsByFundingType("POST_FUNDED");
		then(this.binRepository).should(never()).save(any(Bin.class));
	}

	@Test
	void updateBin_throwsNotFoundException_whenIdNotFound() {
		Long id = 99L;
		given(this.binRepository.findById(id)).willReturn(Optional.empty());

		Assertions.assertThatThrownBy(() -> this.service.updateBin(id, updateReq(111111L, "POST_FUNDED")))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("Bin not found with id: 99");

		then(this.binRepository).should().findById(99L);
		then(this.binRepository).shouldHaveNoMoreInteractions();
	}

	@Test
	void updateBin_updatesOnlyChangedFields_andSaves() {
		Long id = 7L;
		Bin existing = binEntity(id, 111111L, "POST_FUNDED");
		given(this.binRepository.findById(id)).willReturn(Optional.of(existing));
		given(this.binRepository.existsByFundingTypeAndIdNot("PRE_FUNDED_SAME_DAY", id)).willReturn(false);

		UpdateBinRequest req = updateReq(222222L, "PRE_FUNDED_SAME_DAY");

		willAnswer((invocation) -> invocation.getArgument(0)).given(this.binRepository).save(any(Bin.class));

		BinResponse resp = this.service.updateBin(id, req);

		Assertions.assertThat(resp).isNotNull();
		Assertions.assertThat(resp.getId()).isEqualTo(7L);
		Assertions.assertThat(resp.getBin()).isEqualTo(222222L);
		Assertions.assertThat(resp.getFundingType()).isEqualTo("PRE_FUNDED_SAME_DAY");

		then(this.binRepository).should().findById(id);
		then(this.binRepository).should().existsByFundingTypeAndIdNot("PRE_FUNDED_SAME_DAY", id);
		then(this.binRepository).should().save(this.binCaptor.capture());

		Bin saved = this.binCaptor.getValue();
		Assertions.assertThat(saved.getId()).isEqualTo(7L);
		Assertions.assertThat(saved.getBin()).isEqualTo(222222L);
		Assertions.assertThat(saved.getFundingType()).isEqualTo("PRE_FUNDED_SAME_DAY");
	}

	@Test
	void updateBin_noChanges_detectsNoOp_andDoesNotSave() {
		Long id = 3L;
		Bin existing = binEntity(id, 111111L, "POST_FUNDED");
		given(this.binRepository.findById(id)).willReturn(Optional.of(existing));

		UpdateBinRequest req = updateReq(111111L, "POST_FUNDED");

		BinResponse resp = this.service.updateBin(id, req);

		Assertions.assertThat(resp).isNotNull();
		Assertions.assertThat(resp.getId()).isEqualTo(3L);
		Assertions.assertThat(resp.getBin()).isEqualTo(111111L);
		Assertions.assertThat(resp.getFundingType()).isEqualTo("POST_FUNDED");

		then(this.binRepository).should().findById(id);
		then(this.binRepository).should(never()).save(any(Bin.class));
	}

	@Test
	void updateBin_onlyFundingTypeChanged_savesOnce() {
		Long id = 4L;
		Bin existing = binEntity(id, 546810L, "POST_FUNDED");
		given(this.binRepository.findById(id)).willReturn(Optional.of(existing));
		given(this.binRepository.existsByFundingTypeAndIdNot("PRE_FUNDED_NEXT_DAY", id)).willReturn(false);

		UpdateBinRequest req = updateReq(null, "PRE_FUNDED_NEXT_DAY");

		willAnswer((invocation) -> invocation.getArgument(0)).given(this.binRepository).save(any(Bin.class));

		BinResponse resp = this.service.updateBin(id, req);

		Assertions.assertThat(resp).isNotNull();
		Assertions.assertThat(resp.getId()).isEqualTo(4L);
		Assertions.assertThat(resp.getBin()).isEqualTo(546810L);
		Assertions.assertThat(resp.getFundingType()).isEqualTo("PRE_FUNDED_NEXT_DAY");

		then(this.binRepository).should().findById(id);
		then(this.binRepository).should().existsByFundingTypeAndIdNot("PRE_FUNDED_NEXT_DAY", id);
		then(this.binRepository).should().save(any(Bin.class));
	}

	@Test
	void updateBin_throwsBadRequest_whenFundingTypeCollidesWithAnotherRow() {
		Long id = 8L;
		Bin existing = binEntity(id, 546810L, "POST_FUNDED");
		given(this.binRepository.findById(id)).willReturn(Optional.of(existing));
		given(this.binRepository.existsByFundingTypeAndIdNot("PRE_FUNDED_NEXT_DAY", id)).willReturn(true);

		UpdateBinRequest req = updateReq(null, "PRE_FUNDED_NEXT_DAY");

		Assertions.assertThatThrownBy(() -> this.service.updateBin(id, req))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("Funding type already exists: PRE_FUNDED_NEXT_DAY");

		then(this.binRepository).should().findById(id);
		then(this.binRepository).should().existsByFundingTypeAndIdNot("PRE_FUNDED_NEXT_DAY", id);
		then(this.binRepository).should(never()).save(any(Bin.class));
	}

}
