CREATE PROCEDURE [dbo].[usp_PaymentEvent_UpdateStatus]
    @PaymentEventId bigint
   ,@Status varchar(50)
   ,@PreviousStatus varchar(50)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY

        -- Update the payee status
        UPDATE
            [dbo].[PaymentEvent]
        SET
            [CurrentStatus] = @Status
           ,[LastModifiedDateTime] = SYSDATETIME()
        WHERE
            [Id] = @PaymentEventId
            AND [CurrentStatus] = @PreviousStatus;
            
        -- if a record was inserted we also want to insert a status tracking record
        IF @@ROWCOUNT <> 0
        BEGIN
            INSERT INTO [dbo].[PaymentEventStatusTracking] (
                [PaymentEventId]
               ,[Status]
            ) VALUES (
                @PaymentEventId
               ,@Status
            );
   
            SELECT 
                Id
                ,PaymentId
                ,TransactionId
                ,TransactionEventId
                ,EventStatus
                ,EventDateTime
                ,EventReasonCode
                ,EventReasonDescription
                ,CurrentStatus
                ,CreatedDateTime
                ,LastModifiedDateTime
                ,EventPaymentType
                ,EventTrackingNumber
            FROM
                [dbo].[PaymentEvent]
            WHERE
                [Id] = @PaymentEventId;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH

END