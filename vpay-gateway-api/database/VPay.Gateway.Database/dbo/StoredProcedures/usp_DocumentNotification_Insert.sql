CREATE PROCEDURE [dbo].[usp_DocumentNotification_Insert]
     @TransactionId BIGINT
    ,@ArchiveDetailId BIGINT
    ,@ClientCode VARCHAR(10)
    ,@BillingEntityCode VARCHAR(20)
    ,@BillingEntityType VARCHAR(20)
    ,@Status VARCHAR(64)
    ,@PaymentId VARCHAR(100) = ''
    ,@DocumentDescription VARCHAR(100) = ''
    ,@DocumentType VARCHAR(100) = ''
    ,@DocumentName VARCHAR(256) = ''
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO [dbo].[DocumentNotification] (
             [TransactionId]
            ,[ArchiveDetailId]
            ,[ClientCode]
            ,[BillingEntityCode]
            ,[CurrentStatus]
            ,[PaymentId]
            ,[DocumentDescription]
            ,[DocumentType]
            ,[DocumentName]
        ) VALUES (
             @TransactionId
            ,@ArchiveDetailId
            ,@ClientCode
            ,@BillingEntityCode
            ,@Status
            ,@PaymentId
            ,@DocumentDescription
            ,@DocumentType
            ,@DocumentName
        );

        DECLARE @id BIGINT = SCOPE_IDENTITY();

        -- also insert the status tracking record with the current status
        INSERT INTO [dbo].[DocumentNotificationStatusTracking] (
             [DocumentNotificationId]
            ,[Status]
        ) VALUES (
             @id
            ,@Status
        );

        -- return the new record
        EXEC [dbo].[usp_DocumentNotification_GetByDocument] @TransactionId, @ArchiveDetailId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
