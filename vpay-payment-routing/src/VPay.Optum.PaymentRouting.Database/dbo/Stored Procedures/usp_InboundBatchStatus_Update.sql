CREATE PROCEDURE [dbo].[usp_InboundBatchStatus_Update]
    @InboundBatchId BIGINT,
    @Status VARCHAR(100)
AS
BEGIN

    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    BEGIN TRANSACTION
    BEGIN TRY
 
        UPDATE  dbo.InboundBatch
        SET     [Status] = @Status,
                LastModifiedDateTime = SYSDATETIME()
        WHERE   Id = @InboundBatchId
                AND [Status] != @Status;
        Select @@ROWCOUNT as RowsAffected

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
