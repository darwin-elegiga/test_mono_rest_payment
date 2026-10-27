CREATE PROCEDURE [dbo].[usp_InboundHeader_Insert]
    (
    @OptumBatchId VARCHAR(49),
    @Payer835Id VARCHAR(5) ,
    @PayerSubpayerId VARCHAR(10) ,   
    @FileProcId VARCHAR(100) ,
    @PayerAlias VARCHAR(10) ,
    @PaymentClob VARCHAR(max) 
)
AS
BEGIN
SET NOCOUNT ON;

DECLARE @InboundBatchId BIGINT;
EXEC @InboundBatchId = [dbo].usp_InboundBatch_Insert @OptumBatchId, @Payer835Id, null, null;

DECLARE @ClobStorageId BIGINT;
DECLARE @ClobStorageTable TABLE (Id BIGINT);

BEGIN TRAN
    BEGIN TRY
        INSERT [dbo].ClobStorage (PaymentClob)
        OUTPUT inserted.Id INTO @ClobStorageTable
        SELECT @PaymentClob;

        SELECT TOP 1 @ClobStorageId=Id FROM @ClobStorageTable

        INSERT [dbo].[InboundHeader]
            ( 
            InboundBatchId,     
            ClobStorageId,
            FileProcId ,
            PayerAlias ,
            PayerSubpayerId
            )
        SELECT @InboundBatchId,
            @ClobStorageId,
            @FileProcId ,
            @PayerAlias ,
            @PayerSubpayerId
         ;
  END TRY
    BEGIN CATCH
    
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;

        IF XACT_STATE() <> 0
            ROLLBACK;

        THROW;
    END CATCH

IF @@TRANCOUNT > 0
    COMMIT TRAN;

SELECT @InboundBatchId as InboundBatchId;
RETURN @InboundBatchId;
END
