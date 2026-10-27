CREATE PROCEDURE [dbo].[usp_PaymentEvent_Insert]
    @PaymentId BIGINT,
    @TransactionId BIGINT,
    @TransactionEventId BIGINT,
    @EventStatus VARCHAR(10),
    @EventDateTime DATETIME2(7),
    @EventReasonCode VARCHAR(4),
    @EventReasonDescription VARCHAR(50),
    @EventPaymentType VARCHAR(10) = '',
    @EventTrackingNumber VARCHAR(100) = NULL
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY

        INSERT INTO [dbo].[PaymentEvent] (
            [PaymentId]
           ,[TransactionId]
           ,[TransactionEventId]
           ,[EventStatus]
           ,[EventDateTime]
           ,[EventReasonCode]
           ,[EventReasonDescription]
           ,[EventPaymentType]
           ,[EventTrackingNumber]
        )
        SELECT 
            @PaymentId
            ,@TransactionId
            ,@TransactionEventId
            ,@EventStatus
            ,@EventDateTime
            ,@EventReasonCode
            ,@EventReasonDescription
            ,@EventPaymentType
            ,@EventTrackingNumber
        WHERE NOT EXISTS
        (	SELECT 1 FROM dbo.[PaymentEvent] t WITH(UPDLOCK, HOLDLOCK)
            WHERE t.PaymentId = @PaymentId AND t.TransactionEventId = @TransactionEventId
        )
   
        EXEC [dbo].[usp_PaymentEvent_GetByPaymentIdAndTransactionEventId] @PaymentId, @TransactionEventId

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH

END
GO
