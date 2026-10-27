CREATE PROCEDURE [dbo].usp_InboundBatch_Insert
    @OptumBatchId VARCHAR(49) ,
    @Payer835Id VARCHAR(5),
    @TotalPaymentCount INT NULL,
    @TotalPaymentAmount  DECIMAL(20,2) NULL
AS
BEGIN
SET NOCOUNT ON;
SET XACT_ABORT ON;
 
DECLARE @InboundBatchId BIGINT,
        @UpdatePayer    BIT,
        @UpdateTotals   BIT

SELECT      @InboundBatchId = Id, 
            @UpdatePayer = CAST(  CASE WHEN ( Payer835Id = '' AND @Payer835Id IS NOT NULL ) THEN 1 ELSE 0 END  AS BIT),
            @UpdateTotals = CAST( CASE WHEN
                     ( TotalPaymentCount  IS NULL AND @TotalPaymentCount  IS NOT NULL ) OR
                     ( TotalPaymentAmount IS NULL AND @TotalPaymentAmount IS NOT NULL ) 
                THEN 1 ELSE 0 END AS BIT)
FROM        dbo.InboundBatch
WHERE       OptumBatchId = @OptumBatchId;
BEGIN TRAN
BEGIN TRY
    IF (@InboundBatchId is NULL)
    BEGIN

        INSERT dbo.InboundBatch (OptumBatchId, Payer835Id, TotalPaymentCount, TotalPaymentAmount, [Status], LastModifiedDateTime)
        SELECT @OptumBatchId, IsNull(@Payer835Id,''), @TotalPaymentCount, @TotalPaymentAmount, 'BatchInitiated', SYSDATETIME();
        SET @InboundBatchId =SCOPE_IDENTITY();

    END
    ELSE 
    BEGIN
        IF (IsNull(@UpdatePayer,0) = 1)
        BEGIN 
            UPDATE  dbo.InboundBatch
            SET     LastModifiedDateTime = SYSDATETIME(),
                    Payer835Id = @Payer835Id
            WHERE   Id = @InboundBatchId;
        END
        IF (IsNull(@UpdateTotals,0) = 1)
        BEGIN 
            UPDATE  dbo.InboundBatch
            SET     LastModifiedDateTime = SYSDATETIME(),
                    TotalPaymentCount = @TotalPaymentCount, 
                    TotalPaymentAmount = @TotalPaymentAmount
            WHERE   Id = @InboundBatchId;
        END
    END

    IF @@TRANCOUNT > 0 COMMIT TRAN;
END TRY
BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        THROW;
END CATCH
SELECT @InboundBatchId as InboundBatchId;
RETURN @InboundBatchId;
END
