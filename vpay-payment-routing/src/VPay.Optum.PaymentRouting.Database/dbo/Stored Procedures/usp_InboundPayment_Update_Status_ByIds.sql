CREATE PROCEDURE [dbo].[usp_InboundPayment_Update_Status_ByIds]
    @InboundPaymentIds IdTable READONLY,
    @Status VARCHAR(20),
    @ModifiedBy VARCHAR(50)
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    BEGIN TRANSACTION
    BEGIN TRY

        UPDATE  dbo.InboundPayment
        SET     [Status] = @Status,
                LastModifiedDateTime = SYSDATETIME(),
                LastModifiedBy = @ModifiedBy
        WHERE   Id IN (SELECT ID FROM @InboundPaymentIds);
                
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
