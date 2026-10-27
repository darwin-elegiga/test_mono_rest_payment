CREATE PROCEDURE [dbo].[usp_Outbound835File_FinSeg_Insert]
    (
    @OutboundFinSegSequenceId BIGINT,
    @PayerAlias VARCHAR(10), 
    @OutboundRefEO VARCHAR(10),
    @CreatedBy VARCHAR(50),
    @Payments IdTable READONLY
    )
AS
BEGIN
    SET NOCOUNT ON
    BEGIN TRANSACTION
    BEGIN TRY


        DECLARE @FinSegTxtId BIGINT;
        DECLARE @FinSeg835Id BIGINT; 

        -- There are 2 files to be generated... 
        -- the FINSEG txt that contains metadata about the 835 file 
        -- and the 835 file itself.  This is the way.

        INSERT INTO [dbo].[Outbound835File] (PayerAlias, OutboundRefEO, [Status], CreatedBy, CreatedDateTime, LastModifiedBy, LastModifiedDateTime, OutboundFinSegSequenceId)
        VALUES       (@PayerAlias, @OutboundRefEO, 'FinSegTxtQueued', @CreatedBy, SYSDATETIME(), @CreatedBy, SYSDATETIME(), @OutboundFinSegSequenceId);

        SET @FinSegTxtId = SCOPE_IDENTITY();

        INSERT INTO [dbo].[Outbound835File] (PayerAlias, OutboundRefEO, [Status], CreatedBy, CreatedDateTime, LastModifiedBy, LastModifiedDateTime, OutboundFinSegSequenceId)
        VALUES       (@PayerAlias, @OutboundRefEO, 'FinSeg835Queued', @CreatedBy, SYSDATETIME(), @CreatedBy, SYSDATETIME(), @OutboundFinSegSequenceId);

        SET @FinSeg835Id = SCOPE_IDENTITY();
 
        -----
        INSERT INTO [dbo].[Outbound835FileTransaction] (InboundPaymentId, Outbound835FileId, CreatedDateTime)
        SELECT Id, @FinSegTxtId, SYSDATETIME()
        FROM @Payments
        UNION
        SELECT Id, @FinSeg835Id, SYSDATETIME()
        FROM @Payments;

        UPDATE [dbo].[InboundPayment]
        SET [Status] = 'FinSegQueued', LastModifiedBy = @CreatedBy, LastModifiedDateTime = SYSDATETIME()
        WHERE Id IN (select Id from @Payments);


        COMMIT TRANSACTION

        SELECT @FinSegTxtId as OutboundFinSegTxtFileId, @FinSeg835Id AS OutboundFinSeg835FileId;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END