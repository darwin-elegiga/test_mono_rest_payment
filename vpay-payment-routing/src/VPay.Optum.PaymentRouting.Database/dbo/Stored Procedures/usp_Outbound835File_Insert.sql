CREATE PROCEDURE [dbo].[usp_Outbound835File_Insert]
    (
    @PayerAlias varchar(10), 
    @OutboundRefEO varchar(10),
    @CreatedBy varchar(50),
    @Payments IdTable READONLY
    )
AS
BEGIN
    SET NOCOUNT ON
    BEGIN TRANSACTION
    BEGIN TRY
        INSERT INTO [dbo].[Outbound835File] (PayerAlias, OutboundRefEO, [Status], CreatedBy, CreatedDateTime, LastModifiedBy, LastModifiedDateTime)
        VALUES (@PayerAlias, @OutboundRefEO, 'QUEUED', @CreatedBy, SYSDATETIME(), @CreatedBy, SYSDATETIME())
    
        DECLARE @Outbound835FileId bigint;
        SET @Outbound835FileId = SCOPE_IDENTITY();

        INSERT INTO [dbo].[Outbound835FileTransaction] (InboundPaymentId, Outbound835FileId, CreatedDateTime)
        SELECT Id, @Outbound835FileId, SYSDATETIME()
        FROM @Payments

        UPDATE [dbo].[InboundPayment]
        SET [Status] = 'QUEUED835GEN', LastModifiedBy = @CreatedBy, LastModifiedDateTime = SYSDATETIME()
        where Id in (select *
        from @Payments)

        COMMIT TRANSACTION

        SELECT @Outbound835FileId as Outbound835FileId;
        RETURN @Outbound835FileId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
