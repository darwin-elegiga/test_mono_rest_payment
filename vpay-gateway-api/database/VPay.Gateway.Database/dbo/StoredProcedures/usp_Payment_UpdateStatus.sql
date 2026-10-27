CREATE PROCEDURE [dbo].[usp_Payment_UpdateStatus]
    @PaymentId bigint
   ,@Status varchar(50)
   ,@PreviousStatus varchar(50)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY

        -- Update the payment status
        UPDATE
            [dbo].[Payment]
        SET
            [LastSentStatus] = @Status
           ,[LastModifiedDateTime] = SYSDATETIME()
        WHERE
            [Id] = @PaymentId
            AND [LastSentStatus] = @PreviousStatus;
            
        -- if a record was inserted we also want to insert a status tracking record
        IF @@ROWCOUNT <> 0
        BEGIN
            INSERT INTO [dbo].[PaymentStatusTracking] (
                [PaymentId]
               ,[Status]
            ) VALUES (
                @PaymentId
               ,@Status
            );
   
            SELECT 
                Id
                , ClientCode
                , ClientPaymentId
                , LastSentStatus
                , CreatedDateTime
                , LastModifiedDateTime
                , CurrentBalance
            FROM
                dbo.Payment
            WHERE
                [Id] = @PaymentId;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH

END
GO
