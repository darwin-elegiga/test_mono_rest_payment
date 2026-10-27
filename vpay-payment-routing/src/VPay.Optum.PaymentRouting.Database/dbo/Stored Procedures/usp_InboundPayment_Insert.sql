CREATE    PROCEDURE dbo.usp_InboundPayment_Insert
    (
    @OptumBatchId VARCHAR(49),
    @Payer835Id VARCHAR(5) ,
    @FileProcId VARCHAR(100) ,
    @PayerAlias VARCHAR(20) ,
    @ProvTIN VARCHAR(10) ,
    @PayerSubpayerId VARCHAR(10) ,
    @ZipCode VARCHAR(10) ,
    @UserKey VARCHAR(100) , 
    @PaymentAmount DECIMAL(20,2) ,
    @PaymentClob VARCHAR(max),
    @RemitMethod VARCHAR(10)
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

        INSERT [dbo].ClobStorage ( PaymentClob)
        OUTPUT inserted.Id INTO @ClobStorageTable
        SELECT @PaymentClob;

        SELECT TOP 1 @ClobStorageId=Id FROM @ClobStorageTable

        INSERT [dbo].[InboundPayment]
            ( 
            InboundBatchId,     
            ClobStorageId,
            [UserKey] ,
            Payer835Id ,
            FileProcId ,
            PayerAlias ,
            ProvTIN ,
            PayerSubpayerId ,
            ZipCode ,
            PaymentAmount,
            RemitMethod
            )
        SELECT @InboundBatchId,
            @ClobStorageId,
            @UserKey , 
            @Payer835Id ,
            @FileProcId ,
            @PayerAlias ,
            @ProvTIN ,
            @PayerSubpayerId ,
            @ZipCode ,
            @PaymentAmount,
            @RemitMethod;
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