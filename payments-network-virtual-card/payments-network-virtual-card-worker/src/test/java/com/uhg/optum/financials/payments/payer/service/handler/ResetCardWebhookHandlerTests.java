package com.uhg.optum.financials.payments.payer.service.handler;

import com.uhg.optum.financials.payments.payer.dto.callback.ResetCardData;
import com.uhg.optum.financials.payments.payer.model.AuthExtract;
import com.uhg.optum.financials.payments.payer.model.CardReset;
import com.uhg.optum.financials.payments.payer.model.PaymentDetails;
import com.uhg.optum.financials.payments.payer.model.PaymentRequest;
import com.uhg.optum.financials.payments.payer.model.VirtualCard;
import com.uhg.optum.financials.payments.payer.repository.AuthExtractRepository;
import com.uhg.optum.financials.payments.payer.repository.CardCreationRepository;
import com.uhg.optum.financials.payments.payer.repository.CardResetRepository;
import com.uhg.optum.financials.payments.payer.repository.VirtualCardsRepository;
import com.uhg.optum.financials.payments.payer.service.WebhookClient;
import com.uhg.optum.financials.payments.payer.service.impl.VirtualCardBalanceService;
import com.uhg.optum.financials.payments.payer.util.AuditLogger;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import com.uhg.optum.financials.payments.payer.util.VirtualCardUtil;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.BDDMockito;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.test.util.ReflectionTestUtils;

import java.math.BigDecimal;
import java.util.Optional;
import java.util.UUID;

import static com.uhg.optum.financials.payments.payer.enums.CardOperationStatus.AUTH_RESET;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;

@ExtendWith(MockitoExtension.class)
public class ResetCardWebhookHandlerTests {

	@Mock
	private CardResetRepository cardResetRepository;

	@Mock
	private LogUtility logUtility;

	@Mock
	private AuditLogger auditLogger;

	@Mock
	private VirtualCardsRepository virtualCardsRepository;

	@Mock
	private VirtualCardUtil virtualCardUtil;

	@Mock
	private VirtualCardBalanceService virtualCardBalanceService;

	@Mock
	private CardCreationRepository cardCreationRepository;

	@Mock
	private AuthExtractRepository authExtractRepository;

	@Mock
	private WebhookClient webhookClient;

	@InjectMocks
	private ResetCardWebhookHandler handler;

	@BeforeEach
	void setup() {
		ReflectionTestUtils.setField(this.handler, "cardAuthReversedNotificationEndpoint", "http://dummy-endpoint");
	}

	private ResetCardData buildResetCardData(UUID cardGuid, String receiptId) {
		ResetCardData data = new ResetCardData();
		ReflectionTestUtils.setField(data, "cardGuid", cardGuid);
		ReflectionTestUtils.setField(data, "receiptId", receiptId);
		ReflectionTestUtils.setField(data, "resetPurpose", "TEST_PURPOSE");
		return data;
	}

	@Test
	void process_shouldHandleAuthResetFlow() {
		UUID cardGuid = UUID.randomUUID();
		String receiptId = "receipt123";
		ResetCardData resetCardData = buildResetCardData(cardGuid, receiptId);

		CardReset cardReset = new CardReset();
		cardReset.setStatus(AUTH_RESET.name());
		cardReset.setReceiptId(receiptId);

		VirtualCard virtualCard = new VirtualCard();
		ReflectionTestUtils.setField(virtualCard, "paymentId", "pay123");

		AuthExtract authExtract = new AuthExtract();
		ReflectionTestUtils.setField(authExtract, "bin", "123456");
		ReflectionTestUtils.setField(authExtract, "authAmount", BigDecimal.TEN);
		ReflectionTestUtils.setField(authExtract, "maskedCardNumber", "XXXX1234");
		ReflectionTestUtils.setField(authExtract, "receiptId", receiptId);

		PaymentRequest paymentRequest = new PaymentRequest();
		PaymentDetails paymentDetails = new PaymentDetails();
		ReflectionTestUtils.setField(paymentDetails, "issuerPaymentId", "issuer123");
		ReflectionTestUtils.setField(paymentDetails, "id", "payDetailsId");
		ReflectionTestUtils.setField(paymentRequest, "paymentDetails", paymentDetails);

		BDDMockito.when(this.cardResetRepository.findTopByReceiptIdAndCardGuidOrderByCreatedAtDesc(receiptId, cardGuid))
			.thenReturn(cardReset);
		BDDMockito.when(this.virtualCardsRepository.findByCardGuid(cardGuid)).thenReturn(Optional.of(virtualCard));
		BDDMockito.when(this.authExtractRepository.findByReceiptId(receiptId)).thenReturn(authExtract);
		BDDMockito.when(this.virtualCardUtil.updatePaymentRequestStatus("pay123", null)).thenReturn(paymentRequest);

		this.handler.process(resetCardData);

		BDDMockito.verify(this.cardResetRepository).save(any(CardReset.class));
		BDDMockito.verify(this.authExtractRepository).save(any(AuthExtract.class));
		BDDMockito.verify(this.auditLogger).logCardReset(any(), eq(cardGuid), eq("pay123"), eq(receiptId));
	}

}
