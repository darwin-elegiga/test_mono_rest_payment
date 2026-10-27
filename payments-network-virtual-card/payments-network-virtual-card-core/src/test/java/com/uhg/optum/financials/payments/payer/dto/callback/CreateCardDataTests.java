package com.uhg.optum.financials.payments.payer.dto.callback;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import static org.assertj.core.api.BDDAssertions.then;

class CreateCardDataTests {

	@Test
	@DisplayName("should deserialize autoActivateCard without affecting existing create-card fields")
	void shouldDeserializeAutoActivateCardWithoutAffectingExistingCreateCardFields() throws Exception {
		// Arrange
		String json = """
				{
				  \"clientRequestId\": \"f1a8f5fa-87ab-4060-918b-9289a23d4e35\",
				  \"cardGuid\": \"84d74b38-9258-4f03-a112-b1ca76d5de56\",
				  \"initialBalance\": {
				    \"amount\": 100,
				    \"currency\": \"USD\"
				  },
				  \"spendRules\": [\"MULTI_USE\"],
				  \"singleSpend\": false,
				  \"autoActivateCard\": false
				}
				""";
		ObjectMapper objectMapper = new ObjectMapper();

		// Act
		CreateCardData createCardData = objectMapper.readValue(json, CreateCardData.class);

		// Assert
		then(createCardData.getClientRequestId()).hasToString("f1a8f5fa-87ab-4060-918b-9289a23d4e35");
		then(createCardData.getCardGuid()).hasToString("84d74b38-9258-4f03-a112-b1ca76d5de56");
		then(createCardData.getInitialBalance()).isNotNull();
		then(createCardData.getInitialBalance().getAmount()).isEqualTo(100L);
		then(createCardData.getInitialBalance().getCurrency()).isEqualTo("USD");
		then(createCardData.getSpendRules()).containsExactly("MULTI_USE");
		then(createCardData.getSingleSpend()).isFalse();
		then(createCardData.getAutoActivateCard()).isFalse();
	}

	@Test
	@DisplayName("should support java serialization for create-card callback payload")
	void shouldSupportJavaSerializationForCreateCardCallbackPayload() throws Exception {
		// Arrange
		CreateCardData createCardData = new CreateCardData();
		createCardData.setClientRequestId(java.util.UUID.randomUUID());
		createCardData.setCardGuid(java.util.UUID.randomUUID());
		createCardData.setSingleSpend(Boolean.FALSE);
		createCardData.setAutoActivateCard(Boolean.TRUE);
		CreateCardData.InitialBalance initialBalance = new CreateCardData.InitialBalance();
		initialBalance.setAmount(100L);
		initialBalance.setCurrency("USD");
		createCardData.setInitialBalance(initialBalance);

		// Act
		byte[] bytes;
		try (java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
				java.io.ObjectOutputStream oos = new java.io.ObjectOutputStream(bos)) {
			oos.writeObject(createCardData);
			oos.flush();
			bytes = bos.toByteArray();
		}

		// Assert
		then(bytes).isNotEmpty();
	}

}
