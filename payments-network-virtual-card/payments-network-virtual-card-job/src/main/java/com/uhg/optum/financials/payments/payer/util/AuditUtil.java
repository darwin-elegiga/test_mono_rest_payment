package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.shared.eventbus.kafka.audit.IAuditPublisher;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

@Component
@Slf4j
public class AuditUtil extends AuditUtilCommon {

	public AuditUtil(IAuditPublisher iAuditPublisher, @Value("${audit.ask-id}") String askId,
			@Value("${audit.ci}") String ci, @Value("${spring.application.name}") String appName,
			@Value("${application.base-domain}") String hostName, @Value("${audit.app-version}") String version) {
		super(iAuditPublisher, askId, ci, appName, hostName.replace("https://", "").replace("http://", ""), version);
	}

}
