CREATE PROCEDURE [dbo].[usp_Payment_Upsert]
    @ClientCode varchar(10),
    @ClientPaymentId varchar(100),
    @CurrentBalance decimal(18,2) = 0
AS
BEGIN
    SET XACT_ABORT, NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRAN
        
        UPDATE dbo.Payment
        SET
            CurrentBalance = @CurrentBalance,
            LastModifiedDateTime = SYSDATETIME()
        WHERE
            ClientCode = @ClientCode AND ClientPaymentId = @ClientPaymentId

                     
        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO dbo.Payment
                (
                 ClientCode,
                 ClientPaymentId,
                 CurrentBalance
                )
            VALUES
                (
                 @ClientCode,
                 @ClientPaymentId,
                 @CurrentBalance
                )
        END

        EXEC [dbo].[usp_Payment_GetByClientCodeAndPaymentId] @ClientCode, @ClientPaymentId
    

        COMMIT TRAN
    END TRY
    BEGIN CATCH
        IF(@@TRANCOUNT > 0)
            ROLLBACK TRAN;

        THROW;
    END CATCH
END