CREATE PROCEDURE [dbo].[usp_ApMessage_GetListByTransactionId]
    @TransactionId BIGINT,
    @ApMessageType VARCHAR(10) = NULL,
    @ApRoutingType VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  [Id],
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
        AND (@ApMessageType IS NULL OR [ApMessageType] = @ApMessageType)
        AND (@ApRoutingType IS NULL OR [ApRoutingType] = @ApRoutingType)
    ORDER BY [Id] DESC;
END
GO