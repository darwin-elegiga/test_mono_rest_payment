/*
    AUTH: judah burke
    PURP: Update TotalPaymentAmount and TotalPaymentCount to current values
    THROWS: 51001 if record does not exist for [Id] = @InboundBatchId
    THROWS: 51002 if record for [Id] = @InboundBatchId has [Status] = 'BatchComplete'
*/
CREATE PROCEDURE [dbo].[usp_InboundBatch_Update_Totals]
    @InboundBatchId BIGINT,
    @TotalPaymentCount INT,
    @TotalPaymentAmount DECIMAL(20,2)
AS
BEGIN

    SET XACT_ABORT ON;
    SET NOCOUNT ON;

    BEGIN TRANSACTION
    BEGIN TRY
        DECLARE @ActualId BIGINT,
                @ActualStatus VARCHAR(20)
        SELECT  @ActualId = b.[Id], 
                @ActualStatus = b.[Status]
        FROM    [dbo].[InboundBatch] b
        WHERE   b.[Id] = @InboundBatchId

        IF @ActualId IS NULL OR @ActualId <> @InboundBatchId
            THROW 51001, 'The record does not exist for given @InboundBatchId', 1
            
        IF @ActualStatus = 'BatchComplete'
            THROW 51002, 'Cannot update [InboundBatch].[TotalPaymentCount] and [TotalPaymentAmount] if related [InboundBatch].[Status] is ''BatchComplete''', 1


        UPDATE  [dbo].[InboundBatch]
        SET     [TotalPaymentCount] = @TotalPaymentCount,
                [TotalPaymentAmount] = @TotalPaymentAmount,
                [LastModifiedDateTime] = SYSDATETIME()
        WHERE   [Id] = @InboundBatchId;
                
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END
