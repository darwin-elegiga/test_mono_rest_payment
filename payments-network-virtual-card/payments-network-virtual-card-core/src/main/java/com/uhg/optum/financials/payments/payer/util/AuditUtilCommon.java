package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.shared.client.audit.AppData;
import com.uhg.optum.financials.shared.client.audit.DeviceData;
import com.uhg.optum.financials.shared.client.audit.UserData;
import com.uhg.optum.financials.shared.client.audit.enums.LogClass;
import com.uhg.optum.financials.shared.client.audit.enums.Priv;
import com.uhg.optum.financials.shared.client.audit.enums.Severity;
import com.uhg.optum.financials.shared.eventbus.kafka.audit.IAuditPublisher;
import com.uhg.optum.financials.shared.eventbus.kafka.audit.events.ApplicationErrorEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.audit.events.SystemServiceShutdownEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.audit.events.SystemServiceStartupEvent;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.ContextClosedEvent;
import org.springframework.context.event.EventListener;

import java.net.InetAddress;
import java.net.UnknownHostException;

@Slf4j
public class AuditUtilCommon {

	private final IAuditPublisher iAuditPublisher;

	private final String askId;

	private final String ci;

	private final String appName;

	private final String hostName;

	private final String version;

	private static final AppData appData = new AppData();

	public AuditUtilCommon(IAuditPublisher iAuditPublisher, String askId, String ci, String appName, String hostName,
			String version) {
		this.iAuditPublisher = iAuditPublisher;
		this.appName = appName;
		this.ci = ci;
		this.askId = askId;
		this.hostName = hostName;
		this.version = version;
	}

	@EventListener(ApplicationReadyEvent.class)
	private void runStartUpInit() {
		this.setStaticVariables();
		this.sendStartupEvent();
	}

	@EventListener(ContextClosedEvent.class)
	private void sendShutdownEvents() {
		log.info("Sending Shutdown event to eventbus");
		SystemServiceShutdownEvent systemServiceShutdownEvent = new SystemServiceShutdownEvent();
		systemServiceShutdownEvent.setApplication(appData);
		systemServiceShutdownEvent.setDevice(this.getDeviceData());
		systemServiceShutdownEvent.setSourceUser(this.getUserData());
		systemServiceShutdownEvent.setLogClass(LogClass.SECURITY_AUDIT);
		systemServiceShutdownEvent.setSeverity(Severity.INFO);
		systemServiceShutdownEvent.setReceivedTime(System.currentTimeMillis());
		systemServiceShutdownEvent.setMsg("SystemStop:SUCCESS:" + this.appName);
		log.info(appData.getAskId());
		this.iAuditPublisher.auditSystemServiceShutdown(systemServiceShutdownEvent, null);

	}

	private void sendStartupEvent() {
		setStaticVariables();
		log.info("Sending Startup event to eventbus");
		SystemServiceStartupEvent systemServiceStartupEvent = new SystemServiceStartupEvent();
		systemServiceStartupEvent.setApplication(appData);
		systemServiceStartupEvent.setDevice(this.getDeviceData());
		systemServiceStartupEvent.setSourceUser(this.getUserData());
		systemServiceStartupEvent.setLogClass(LogClass.SECURITY_AUDIT);
		systemServiceStartupEvent.setSeverity(Severity.INFO);
		systemServiceStartupEvent.setReceivedTime(System.currentTimeMillis());
		systemServiceStartupEvent.setMsg("SystemStart:SUCCESS:" + this.appName);
		this.iAuditPublisher.auditSystemServiceStartup(systemServiceStartupEvent, null);
	}

	private void setStaticVariables() {
		appData.setAskId(this.askId);
		appData.setCI(this.ci);
		appData.setName(this.appName);
	}

	private DeviceData getDeviceData() {
		String ipAddress = "0";
		try {
			ipAddress = InetAddress.getLocalHost().getHostAddress().replace(".", "");
		}
		catch (UnknownHostException unknownHostException) {
			log.error("Failed to get the IP Address of the host. IP address will not be available in events");
		}
		DeviceData deviceData = new DeviceData();
		deviceData.setHostname(this.hostName);
		deviceData.setIp4(Long.parseLong(ipAddress));
		deviceData.setVendor(AppConstants.OPTUM);
		deviceData.setVersion(this.version);
		deviceData.setProduct(this.appName);
		return deviceData;
	}

	private UserData getUserData() {
		UserData userData = new UserData();
		userData.setName(this.appName);
		userData.setPriv(Priv.SYSTEM);
		userData.setUid(this.appName);
		return userData;
	}

	public void sendErrorEvent(String errorMessage) {
		log.info("Sending Audit Error event to eventbus");
		ApplicationErrorEvent applicationErrorEvent = new ApplicationErrorEvent();
		applicationErrorEvent.setApplication(appData);
		applicationErrorEvent.setDevice(this.getDeviceData());
		applicationErrorEvent.setLogClass(LogClass.SECURITY_AUDIT);
		applicationErrorEvent.setSeverity(Severity.INFO);
		applicationErrorEvent.setReceivedTime(System.currentTimeMillis());
		applicationErrorEvent.setMsg("Application:ERROR:" + errorMessage);
		this.iAuditPublisher.auditApplicationError(applicationErrorEvent, null);
	}

}
