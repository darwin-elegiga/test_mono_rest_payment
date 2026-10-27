CREATE PROCEDURE [dbo].[usp_ApMessageTrackingStatus_UpdateStatus]
     @ApMessageTrackingId bigint,
     @Status varchar(20),
     @ModifiedBy varchar(50)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    BEGIN TRANSACTION
    BEGIN TRY
        UPDATE [dbo].[ApMessageTracking]
        SET [Status] = @Status, LastModifiedBy = @ModifiedBy, LastModifiedDateTime = SYSDATETIME()
        WHERE Id = @ApMessageTrackingId

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END