package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.constant.AppConstants;
import com.uhg.optum.financials.shared.client.audit.AppData;
import com.uhg.optum.financials.shared.client.audit.DeviceData;
import com.uhg.optum.financials.shared.client.audit.UserData;
import com.uhg.optum.financials.shared.client.audit.enums.Priv;
import com.uhg.optum.financials.shared.eventbus.kafka.audit.IAuditPublisher;
import com.uhg.optum.financials.shared.eventbus.kafka.audit.events.ApplicationErrorEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.audit.events.SystemServiceShutdownEvent;
import com.uhg.optum.financials.shared.eventbus.kafka.audit.events.SystemServiceStartupEvent;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import static org.assertj.core.api.AssertionsForClassTypes.assertThat;
import static org.mockito.BDDMockito.then;
import static org.mockito.Mockito.times;
import java.lang.reflect.Field;
import java.lang.reflect.Method;

class AuditUtilCommonTests {

	@Mock
	private IAuditPublisher iAuditPublisher;

	private AuditUtilCommon auditUtilCommon;

	@BeforeEach
	void setUp() {
		MockitoAnnotations.openMocks(this);
		this.auditUtilCommon = new AuditUtilCommon(this.iAuditPublisher, "askId", "ci", "appName", "hostName",
				"version");
	}

	@Test
	void sendErrorEvent_shouldPublishErrorEvent() {
		this.auditUtilCommon.sendErrorEvent("error occurred");
		then(this.iAuditPublisher).should(times(1))
			.auditApplicationError(org.mockito.ArgumentMatchers.any(ApplicationErrorEvent.class),
					org.mockito.ArgumentMatchers.isNull());
	}

	@Test
	void getDeviceData_shouldReturnDeviceData() throws Exception {
		Method method = this.auditUtilCommon.getClass().getDeclaredMethod("getDeviceData");
		method.setAccessible(true);
		DeviceData deviceData = (DeviceData) method.invoke(this.auditUtilCommon);
		assertThat(deviceData.getHostname()).isEqualTo("hostName");
		assertThat(deviceData.getVendor()).isEqualTo(AppConstants.OPTUM);
		assertThat(deviceData.getVersion()).isEqualTo("version");
		assertThat(deviceData.getProduct()).isEqualTo("appName");
	}

	@Test
	void getUserData_shouldReturnUserData() throws Exception {
		Method method = this.auditUtilCommon.getClass().getDeclaredMethod("getUserData");
		method.setAccessible(true);
		UserData userData = (UserData) method.invoke(this.auditUtilCommon);
		assertThat(userData.getName()).isEqualTo("appName");
		assertThat(userData.getPriv()).isEqualTo(Priv.SYSTEM);
		assertThat(userData.getUid()).isEqualTo("appName");
	}

	@Test
	void sendShutdownEvents_shouldPublishShutdownEvent() throws Exception {
		Method method = this.auditUtilCommon.getClass().getDeclaredMethod("sendShutdownEvents");
		method.setAccessible(true);
		method.invoke(this.auditUtilCommon);
		then(this.iAuditPublisher).should(times(1))
			.auditSystemServiceShutdown(org.mockito.ArgumentMatchers.any(SystemServiceShutdownEvent.class),
					org.mockito.ArgumentMatchers.isNull());
	}

	@Test
	void sendStartupEvent_shouldPublishStartupEvent() throws Exception {
		Method method = this.auditUtilCommon.getClass().getDeclaredMethod("sendStartupEvent");
		method.setAccessible(true);
		method.invoke(this.auditUtilCommon);
		then(this.iAuditPublisher).should(times(1))
			.auditSystemServiceStartup(org.mockito.ArgumentMatchers.any(SystemServiceStartupEvent.class),
					org.mockito.ArgumentMatchers.isNull());
	}

	@Test
	void runStartUpInit_shouldCallStartupMethods() throws Exception {
		Method method = this.auditUtilCommon.getClass().getDeclaredMethod("runStartUpInit");
		method.setAccessible(true);
		method.invoke(this.auditUtilCommon);
		then(this.iAuditPublisher).should(times(1))
			.auditSystemServiceStartup(org.mockito.ArgumentMatchers.any(SystemServiceStartupEvent.class),
					org.mockito.ArgumentMatchers.isNull());
	}

	@Test
	void setStaticVariables_shouldSetAppDataFields() throws Exception {
		Method method = this.auditUtilCommon.getClass().getDeclaredMethod("setStaticVariables");
		method.setAccessible(true);
		method.invoke(this.auditUtilCommon);
		Field field = AuditUtilCommon.class.getDeclaredField("appData");
		field.setAccessible(true);
		AppData appData = (AppData) field.get(null);
		assertThat(appData.getAskId()).isEqualTo("askId");
		assertThat(appData.getCI()).isEqualTo("ci");
		assertThat(appData.getName()).isEqualTo("appName");
	}

}
