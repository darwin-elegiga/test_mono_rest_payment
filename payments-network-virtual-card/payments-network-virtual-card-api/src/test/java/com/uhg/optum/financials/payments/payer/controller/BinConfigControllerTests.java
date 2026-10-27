package com.uhg.optum.financials.payments.payer.controller;

import com.uhg.optum.financials.payments.payer.dto.BinResponse;
import com.uhg.optum.financials.payments.payer.dto.CreateBinRequest;
import com.uhg.optum.financials.payments.payer.dto.UpdateBinRequest;
import com.uhg.optum.financials.payments.payer.service.BinConfigService;
import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;

import org.assertj.core.api.Assertions;

import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class BinConfigControllerTests {

	@Mock
	private BinConfigService binService;

	@InjectMocks
	private BinConfigController controller;

	private BinResponse binResponse(Long id, Long bin, String fundingType) {
		BinResponse r = new BinResponse();
		r.setId(id);
		r.setBin(bin);
		r.setFundingType(fundingType);
		return r;
	}

	@Test
	void testGetAllBins_ReturnsList() {
		List<BinResponse> expected = List.of(binResponse(1L, 111111L, "PRE_FUNDED_NEXT_DAY"),
				binResponse(2L, 546810L, "POST_FUNDED"));
		BDDMockito.when(this.binService.getAllBins()).thenReturn(expected);

		List<BinResponse> actual = this.controller.getAllBins();

		Assertions.assertThat(actual).isNotNull();
		Assertions.assertThat(actual).hasSize(2);
		Assertions.assertThat(actual).isEqualTo(expected);
		BDDMockito.verify(this.binService).getAllBins();
	}

	@Test
	void testSaveBin_Created() {
		CreateBinRequest request = new CreateBinRequest();
		request.setBin(548130L);
		request.setFundingType("POST_FUNDED");

		BinResponse expected = binResponse(10L, 548130L, "POST_FUNDED");
		BDDMockito.when(this.binService.saveBin(any(CreateBinRequest.class))).thenReturn(expected);

		BinResponse actual = this.controller.saveBin(request);

		Assertions.assertThat(actual).isNotNull();
		Assertions.assertThat(actual.getId()).isEqualTo(10L);
		Assertions.assertThat(actual.getBin()).isEqualTo(548130L);
		Assertions.assertThat(actual.getFundingType()).isEqualTo("POST_FUNDED");
		BDDMockito.verify(this.binService).saveBin(request);
	}

	@Test
	void testUpdateBin_UpdatesSuccessfully() {
		Long id = 7L;
		UpdateBinRequest request = new UpdateBinRequest();
		request.setBin(111111L);
		request.setFundingType("PRE_FUNDED_SAME_DAY");

		BinResponse expected = binResponse(id, 111111L, "PRE_FUNDED_SAME_DAY");
		BDDMockito.when(this.binService.updateBin(eq(id), any(UpdateBinRequest.class))).thenReturn(expected);

		BinResponse actual = this.controller.updateBin(id, request);

		Assertions.assertThat(actual).isNotNull();
		Assertions.assertThat(actual.getId()).isEqualTo(7L);
		Assertions.assertThat(actual.getBin()).isEqualTo(111111L);
		Assertions.assertThat(actual.getFundingType()).isEqualTo("PRE_FUNDED_SAME_DAY");
		BDDMockito.verify(this.binService).updateBin(id, request);
	}

	@Test
	void testSaveBin_ServiceThrows_PropagatesException() {
		CreateBinRequest request = new CreateBinRequest();
		request.setBin(111111L);
		request.setFundingType("PRE_FUNDED_SAME_DAY");

		RuntimeException ex = new RuntimeException("BIN already exists");
		BDDMockito.when(this.binService.saveBin(any(CreateBinRequest.class))).thenThrow(ex);

		Assertions.assertThatThrownBy(() -> this.controller.saveBin(request))
			.isInstanceOf(RuntimeException.class)
			.hasMessage("BIN already exists");

		BDDMockito.verify(this.binService).saveBin(request);
	}

	@Test
	void testUpdateBin_ServiceThrows_PropagatesException() {
		Long id = 99L;
		UpdateBinRequest request = new UpdateBinRequest();
		request.setFundingType("POST_FUNDED");

		NotFoundException ex = new NotFoundException("Bin not found with id: 99");
		BDDMockito.when(this.binService.updateBin(eq(id), any(UpdateBinRequest.class))).thenThrow(ex);

		Assertions.assertThatThrownBy(() -> this.controller.updateBin(id, request))
			.isInstanceOf(NotFoundException.class)
			.hasMessage("Bin not found with id: 99");

		BDDMockito.verify(this.binService).updateBin(id, request);
	}

	@Test
	void testSaveBin_WithInvalidFundingType_ThrowsValidationError() {
		CreateBinRequest request = new CreateBinRequest();
		request.setBin(111111L);
		request.setFundingType(""); // Empty string violates @NotBlank

		BadRequestException ex = new BadRequestException("fundingType cannot be blank");
		BDDMockito.when(this.binService.saveBin(any(CreateBinRequest.class))).thenThrow(ex);

		Assertions.assertThatThrownBy(() -> this.controller.saveBin(request))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("fundingType cannot be blank");
	}

	@Test
	void testUpdateBin_WithNegativeBin_ThrowsValidationError() {
		Long id = 1L;
		UpdateBinRequest request = new UpdateBinRequest();
		request.setBin(-100L); // Violates @Positive
		request.setFundingType("PRE_FUNDED");

		BadRequestException ex = new BadRequestException("bin must be a positive number");
		BDDMockito.when(this.binService.updateBin(eq(id), any(UpdateBinRequest.class))).thenThrow(ex);

		Assertions.assertThatThrownBy(() -> this.controller.updateBin(id, request))
			.isInstanceOf(BadRequestException.class)
			.hasMessage("bin must be a positive number");
	}

}
