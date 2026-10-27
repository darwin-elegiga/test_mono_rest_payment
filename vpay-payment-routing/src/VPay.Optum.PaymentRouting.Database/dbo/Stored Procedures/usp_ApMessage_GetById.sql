CREATE PROCEDURE [dbo].[usp_ApMessage_GetById]
    @Id BIGINT
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
    WHERE [Id] = @Id;
END
GO