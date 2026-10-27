CREATE PROCEDURE [dbo].[usp_ApMessage_GetByTransactionIdAndApMessageType]
    @TransactionId BIGINT,
    @ApMessageType VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
            [Id],
            [TransactionId],
            [ClientCode],
            [BillingEntityCode],
            [ApMessageType],
            [ApRoutingType],
            [IsOptumEnrolled],
            [PayerId],
            [SubPayerId],
            [PaymentSource],
            [PaymentType],
            [ProviderNumber],
            [ProviderName],
            [ProviderNpiNumber],
            [UnConsolPayNumber],
            [UnConsolPayAmount],
            [DisplayConsolPayNumber],
            [ConsolAmount],
            [PayMethodCode],
            [SettlementDate],
            [CheckNumber],
            [ReturnReasonCode],
            [RevisedMethodCode],
            [ReOriginatedDate],
            [CreatedBy],
            [CreatedDateTime],
            [ModifiedBy],
            [ModifiedDateTime]
    FROM [dbo].[ApMessage]
    WHERE [TransactionId] = @TransactionId
      AND [ApMessageType] = @ApMessageType
    ORDER BY [Id] DESC;
END
GO