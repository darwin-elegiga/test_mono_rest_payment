CREATE PROCEDURE [dbo].[usp_ApMessage_Insert]
     @TransactionId BIGINT,
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
     @CreatedBy VARCHAR(50)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    BEGIN TRANSACTION
    BEGIN TRY
        DECLARE @ApMessageId BIGINT;

        -- Serialize the existence check to avoid race-condition duplicate inserts
        SELECT @ApMessageId = A.[Id]
        FROM [dbo].[ApMessage] A WITH (UPDLOCK, HOLDLOCK)
        WHERE A.[TransactionId] = @TransactionId
          AND A.[ApMessageType] = @ApMessageType;

        IF @ApMessageId IS NULL
        BEGIN
            INSERT [dbo].[ApMessage]
            (
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
            )
            SELECT
                @TransactionId,
                @ClientCode,
                @BillingEntityCode,
                @ApMessageType,
                @ApRoutingType,
                @IsOptumEnrolled,
                @PayerId,
                @SubPayerId,
                @PaymentSource,
                @PaymentType,
                @ProviderNumber,
                @ProviderName,
                @ProviderNpiNumber,
                @UnConsolPayNumber,
                @UnConsolPayAmount,
                @DisplayConsolPayNumber,
                @ConsolAmount,
                @PayMethodCode,
                @SettlementDate,
                @CheckNumber,
                @ReturnReasonCode,
                @RevisedMethodCode,
                @ReOriginatedDate,
                @CreatedBy,
                SYSDATETIME(),
                NULL,
                NULL;

            SET @ApMessageId = SCOPE_IDENTITY();
        END

        COMMIT TRANSACTION;

        SELECT @ApMessageId AS ApMessageId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
GO