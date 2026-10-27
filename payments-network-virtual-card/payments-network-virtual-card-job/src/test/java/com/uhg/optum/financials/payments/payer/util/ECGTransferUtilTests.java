package com.uhg.optum.financials.payments.payer.util;

import com.jcraft.jsch.JSchException;
import com.jcraft.jsch.SftpException;
import com.uhg.optum.financials.shared.core.service.SFTPTransferService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.util.ReflectionTestUtils;

import java.io.File;
import java.io.FileNotFoundException;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.BDDMockito.given;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.times;

@ExtendWith(MockitoExtension.class)
class ECGTransferUtilTests {

	@Mock
	private SFTPTransferService ecgTransferService;

	@InjectMocks
	private ECGTransferUtil ecgTransferUtil;

	@BeforeEach
	void setUp() {
		ReflectionTestUtils.setField(this.ecgTransferUtil, "ecgDestination", "/remote/path/");
	}

	@Test
	@DisplayName("givenValidFile_whenTransferToECG_thenReturnsTrueAndCallsTransmitFile")
	void givenValidFile_whenTransferToECG_thenReturnsTrueAndCallsTransmitFile() throws Exception {
		// given
		File file = new File("/tmp/testfile.txt");
		// No exception thrown by transmitFile

		// when
		boolean result = this.ecgTransferUtil.transferToECG(file);

		// then
		assertThat(result).isTrue();
		then(this.ecgTransferService).should(times(1))
			.transmitFile(file.getAbsolutePath(), "/remote/path/" + file.getName());
	}

	@Test
	@DisplayName("givenJSchException_whenTransferToECG_thenReturnsFalse")
	void givenJSchException_whenTransferToECG_thenReturnsFalse() throws Exception {
		// given
		File file = new File("/tmp/testfile.txt");
		given(this.ecgTransferService.transmitFile(file.getAbsolutePath(), "/remote/path/" + file.getName()))
			.willThrow(new JSchException("jsch error"));

		// when
		boolean result = this.ecgTransferUtil.transferToECG(file);

		// then
		assertThat(result).isFalse();
	}

	@Test
	@DisplayName("givenSftpException_whenTransferToECG_thenReturnsFalse")
	void givenSftpException_whenTransferToECG_thenReturnsFalse() throws Exception {
		// given
		File file = new File("/tmp/testfile.txt");
		given(this.ecgTransferService.transmitFile(file.getAbsolutePath(), "/remote/path/" + file.getName()))
			.willThrow(new SftpException(4, "sftp error"));

		// when
		boolean result = this.ecgTransferUtil.transferToECG(file);

		// then
		assertThat(result).isFalse();
	}

	@Test
	@DisplayName("givenFileNotFoundException_whenTransferToECG_thenReturnsFalse")
	void givenFileNotFoundException_whenTransferToECG_thenReturnsFalse() throws Exception {
		// given
		File file = new File("/tmp/testfile.txt");
		given(this.ecgTransferService.transmitFile(file.getAbsolutePath(), "/remote/path/" + file.getName()))
			.willThrow(new FileNotFoundException("file not found"));

		// when
		boolean result = this.ecgTransferUtil.transferToECG(file);

		// then
		assertThat(result).isFalse();
	}

}
