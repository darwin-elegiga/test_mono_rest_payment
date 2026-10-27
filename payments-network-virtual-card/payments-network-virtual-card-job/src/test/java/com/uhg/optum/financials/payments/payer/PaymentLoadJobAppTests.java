package com.uhg.optum.financials.payments.payer;

import com.uhg.optum.financials.payments.payer.service.JobService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.WebApplicationType;

import static org.assertj.core.api.Assertions.assertThatNoException;
import static org.mockito.BDDMockito.then;
import static org.mockito.BDDMockito.willDoNothing;

@ExtendWith(MockitoExtension.class)
class PaymentLoadJobAppTests {

	@Mock
	private JobService jobService;

	@InjectMocks
	private PaymentLoadJobApp paymentLoadJobApp;

	@BeforeEach
	void setUp() {
		this.paymentLoadJobApp = new PaymentLoadJobApp(this.jobService);
	}

	@Test
	@DisplayName("givenJobService_whenRun_thenCheckAndSubmitJobCalled")
	void givenJobService_whenRun_thenCheckAndSubmitJobCalled() {
		// given
		willDoNothing().given(this.jobService).checkAndSubmitJob();
		// when
		this.paymentLoadJobApp.run();
		// then
		then(this.jobService).should().checkAndSubmitJob();
	}

	@Test
	@DisplayName("givenArgs_whenMain_thenSpringApplicationRunCalled")
	void givenArgs_whenMain_thenSpringApplicationRunCalled() {
		// given
		String[] args = new String[] { "arg1", "arg2" };
		try (org.mockito.MockedConstruction<SpringApplication> mocked = Mockito
			.mockConstruction(SpringApplication.class, (mock, context) -> {
				willDoNothing().given(mock).setWebApplicationType(WebApplicationType.NONE);
				Mockito.when(mock.run(args)).thenReturn(null);
			})) {
			// when/then
			assertThatNoException().isThrownBy(() -> PaymentLoadJobApp.main(args));
			SpringApplication mockApp = mocked.constructed().get(0);
			then(mockApp).should().setWebApplicationType(WebApplicationType.NONE);
			Mockito.verify(mockApp).run(args);
		}
	}

	@Test
	@DisplayName("givenJobService_whenConstructor_thenNoException")
	void givenJobService_whenConstructor_thenNoException() {
		// when/then
		assertThatNoException().isThrownBy(() -> new PaymentLoadJobApp(this.jobService));
	}

}
