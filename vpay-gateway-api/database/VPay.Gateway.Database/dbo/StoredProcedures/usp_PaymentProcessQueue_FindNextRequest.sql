CREATE PROCEDURE [dbo].[usp_PaymentProcessQueue_FindNextRequest]
    @ClientCode varchar(10),
    @ClientPaymentId varchar(100)
AS
BEGIN
    SET XACT_ABORT, NOCOUNT ON;
    

    BEGIN TRY
        BEGIN TRAN
                        
        DECLARE @Id bigint;

        IF ( NOT EXISTS ( SELECT 1 FROM dbo.PaymentProcessQueue t WITH(UPDLOCK, HOLDLOCK)
            WHERE t.ClientCode = @ClientCode AND t.ClientPaymentId = @ClientPaymentId))
        BEGIN
            INSERT INTO dbo.PaymentProcessQueue
                (
                 ClientCode,
                 ClientPaymentId
                )
            VALUES
                (
                 @ClientCode,
                 @ClientPaymentId
                )
    
            SET @Id = SCOPE_IDENTITY()
        END
          ELSE
        BEGIN
           --update the LastRequestDateTime so we can know if we need to requeue the message
            UPDATE dbo.PaymentProcessQueue
            SET LastRequestedDateTime = SYSDATETIME()
            WHERE ClientCode = @ClientCode AND ClientPaymentId = @ClientPaymentId
        END
        
        SELECT
            Id
            , ClientCode
            , ClientPaymentId
            , CreatedDateTime
            , LastRequestedDateTime
            , SYSDATETIME() AS CurrentDatabaseTime
        FROM
            [dbo].[PaymentProcessQueue]
        WHERE
            Id = @Id
    

        COMMIT TRAN
    END TRY
    BEGIN CATCH
        IF(@@TRANCOUNT > 0)
            ROLLBACK TRAN;

        THROW;
    END CATCH
END