CREATE PROCEDURE [dbo].[usp_DocumentNotification_Update]
     @TransactionId BIGINT
    ,@ArchiveDetailId BIGINT
    ,@Status VARCHAR(64)
    ,@PreviousStatus VARCHAR(64)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY

        DECLARE @DocumentNotificationId BIGINT = NULL;

        -- Update document notification
        UPDATE
            [dbo].[DocumentNotification]
        SET
             @DocumentNotificationId = [Id]
            ,[CurrentStatus] = @Status
            ,[LastModifiedDateTime] = SYSDATETIME()
        WHERE
            [TransactionId] = @TransactionId
            AND [ArchiveDetailId] = @ArchiveDetailId
            AND [CurrentStatus] = @PreviousStatus;

        -- if a record was inserted, we need to also update the current status on the header record
        IF @@ROWCOUNT <> 0
        BEGIN
            -- insert the new status tracking record
            INSERT INTO [dbo].[DocumentNotificationStatusTracking] (
                 [DocumentNotificationId]
                ,[Status]
            ) VALUES (
                 @DocumentNotificationId
                ,@Status
            );

            EXEC [dbo].[usp_DocumentNotification_GetByDocument] @TransactionId, @ArchiveDetailId;
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
