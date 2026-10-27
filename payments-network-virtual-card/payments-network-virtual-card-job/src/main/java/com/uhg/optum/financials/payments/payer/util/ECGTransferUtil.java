package com.uhg.optum.financials.payments.payer.util;

import com.jcraft.jsch.JSchException;
import com.jcraft.jsch.SftpException;
import com.uhg.optum.financials.shared.core.service.SFTPTransferService;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.io.File;
import java.io.FileNotFoundException;

@Slf4j
@Component
public class ECGTransferUtil {

	@Autowired
	SFTPTransferService ecgTransferService;

	@Value("${sftp.ecg.destination}")
	private String ecgDestination;

	public boolean transferToECG(File file) {
		try {
			log.info("Transferring file Path: {}", file.getAbsolutePath());
			this.ecgTransferService.transmitFile(file.getAbsolutePath(), this.ecgDestination + file.getName());
			log.info("File: {} successfully transferred to ECG Destination Path: {}", file.getName(),
					this.ecgDestination);
			return true;
		}
		catch (JSchException | SftpException | FileNotFoundException ex) {
			log.error("Error in transferring file to ECG {}", ex.getMessage());
			return false;
		}
	}

}
