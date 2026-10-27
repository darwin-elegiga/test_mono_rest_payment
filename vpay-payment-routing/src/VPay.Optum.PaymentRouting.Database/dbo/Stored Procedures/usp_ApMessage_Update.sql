CREATE PROCEDURE [dbo].[usp_ApMessage_Update]
    @Id BIGINT,
    @ClientCode VARCHAR(10),
    @BillingEntityCode VARCHAR(20),
    @ApMessageType VARCHAR(10),
    @ApRoutingType VARCHAR(10),
    @IsOptumEnrolled BIT,
    @PayerId VARCHAR(10),
    @SubPayerId VARCHAR(20),
    @PaymentSource VARCHAR(20),
    @PaymentType VARCHAR(10),
    @ProviderNumber VARCHAR(50),
    @ProviderName VARCHAR(100),
    @ProviderNpiNumber VARCHAR(20),
    @UnConsolPayNumber VARCHAR(100),
    @UnConsolPayAmount VARCHAR(50),
    @DisplayConsolPayNumber VARCHAR(100),
    @ConsolAmount VARCHAR(50),
    @PayMethodCode VARCHAR(20),
    @SettlementDate VARCHAR(20),
    @CheckNumber VARCHAR(20),
    @ReturnReasonCode VARCHAR(20),
    @RevisedMethodCode VARCHAR(20),
    @ReOriginatedDate VARCHAR(20),
    @ModifiedBy VARCHAR(50)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    BEGIN TRANSACTION
    BEGIN TRY
        UPDATE [dbo].[ApMessage]
        SET [ClientCode] = @ClientCode,
            [BillingEntityCode] = @BillingEntityCode,
            [ApMessageType] = @ApMessageType,
            [ApRoutingType] = @ApRoutingType,
            [IsOptumEnrolled] = @IsOptumEnrolled,
            [PayerId] = @PayerId,
            [SubPayerId] = @SubPayerId,
            [PaymentSource] = @PaymentSource,
            [PaymentType] = @PaymentType,
            [ProviderNumber] = @ProviderNumber,
            [ProviderName] = @ProviderName,
            [ProviderNpiNumber] = @ProviderNpiNumber,
            [UnConsolPayNumber] = @UnConsolPayNumber,
            [UnConsolPayAmount] = @UnConsolPayAmount,
            [DisplayConsolPayNumber] = @DisplayConsolPayNumber,
            [ConsolAmount] = @ConsolAmount,
            [PayMethodCode] = @PayMethodCode,
            [SettlementDate] = @SettlementDate,
            [CheckNumber] = @CheckNumber,
            [ReturnReasonCode] = @ReturnReasonCode,
            [RevisedMethodCode] = @RevisedMethodCode,
            [ReOriginatedDate] = @ReOriginatedDate,
            [ModifiedBy] = @ModifiedBy,
            [ModifiedDateTime] = SYSDATETIME()
        WHERE [Id] = @Id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO