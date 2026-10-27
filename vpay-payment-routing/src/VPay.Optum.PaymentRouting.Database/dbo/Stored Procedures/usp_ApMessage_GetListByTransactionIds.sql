CREATE PROCEDURE [dbo].[usp_ApMessage_GetListByTransactionIds]
    @TransactionIds IdTable READONLY,
    @ApMessageType VARCHAR(10) = NULL,
    @ApRoutingType VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  A.[Id],
            A.[TransactionId],
            A.[ClientCode],
            A.[BillingEntityCode],
            A.[ApMessageType],
            A.[ApRoutingType],
            A.[IsOptumEnrolled],
            A.[PayerId],
            A.[SubPayerId],
            A.[PaymentSource],
            A.[PaymentType],
            A.[ProviderNumber],
            A.[ProviderName],
            A.[ProviderNpiNumber],
            A.[UnConsolPayNumber],
            A.[UnConsolPayAmount],
            A.[DisplayConsolPayNumber],
            A.[ConsolAmount],
            A.[PayMethodCode],
            A.[SettlementDate],
            A.[CheckNumber],
            A.[ReturnReasonCode],
            A.[RevisedMethodCode],
            A.[ReOriginatedDate],
            [CreatedBy],
            [CreatedDateTime],
            [ModifiedBy],
            [ModifiedDateTime]
    FROM [dbo].[ApMessage] A
    INNER JOIN @TransactionIds T
        ON T.[Id] = A.[TransactionId]
    WHERE (@ApMessageType IS NULL OR A.[ApMessageType] = @ApMessageType)
      AND (@ApRoutingType IS NULL OR [ApRoutingType] = @ApRoutingType)
    ORDER BY A.[TransactionId], A.[Id] DESC;
END
GO