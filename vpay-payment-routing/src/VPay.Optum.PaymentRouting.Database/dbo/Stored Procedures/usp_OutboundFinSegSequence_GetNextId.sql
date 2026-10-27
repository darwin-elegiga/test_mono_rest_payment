CREATE PROCEDURE [dbo].[usp_OutboundFinSegSequence_GetNextId]
    @PayerAlias varchar(10), 
    @DateStamp VARCHAR(8),
    @CreatedBy varchar(50)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    DECLARE @OutboundFinSegSequenceId TABLE (Id BIGINT);
    
    BEGIN TRANSACTION
    BEGIN TRY

        IF NOT EXISTS ( SELECT * FROM dbo.OutboundFinSegSequence WHERE PayerAlias = @PayerAlias AND @DateStamp = DateStamp)
        BEGIN
            INSERT INTO dbo.OutboundFinSegSequence (PayerAlias, DateStamp, CreatedBy)
            OUTPUT      inserted.Id INTO @OutboundFinSegSequenceId
            VALUES      (@PayerAlias, @DateStamp, @CreatedBy);
        END
            ELSE
        BEGIN
            UPDATE      dbo.OutboundFinSegSequence
            SET         CurrentSequenceNumber = CurrentSequenceNumber + 1,
                        LastModifiedBy = @CreatedBy,
                        LastModifiedDateTime = SYSDATETIME()
            OUTPUT      inserted.Id INTO @OutboundFinSegSequenceId
            WHERE       PayerAlias = @PayerAlias AND @DateStamp = DateStamp
        END

        COMMIT TRANSACTION;

        SELECT      Id AS OutboundFinSegSequenceId,
                    PayerAlias,
                    DateStamp,
                    CurrentSequenceNumber,
                    CreatedBy,
                    CreatedDateTime,
                    [LastModifiedBy],
                    [LastModifiedDateTime]
        FROM        dbo.OutboundFinSegSequence 
        WHERE       Id = (SELECT TOP 1 Id FROM @OutboundFinSegSequenceId);

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
    
    RETURN 0;
END
