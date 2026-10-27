CREATE PROCEDURE [dbo].[usp_DocumentNotification_GetByDocument]
     @TransactionId bigint
    ,@ArchiveDetailId bigint
AS
BEGIN
    SET NOCOUNT ON;

    SELECT    
         [Id]
        ,[TransactionId]
        ,[ArchiveDetailId]
        ,[ClientCode]
        ,[BillingEntityCode]
        ,[BillingEntityType]
        ,[CurrentStatus]
        ,[CreatedDateTime]
        ,[LastModifiedDateTime]
        ,[PaymentId]
        ,[DocumentDescription]
        ,[DocumentType]
        ,[DocumentName]
    FROM
        [dbo].[DocumentNotification]
    WHERE
        [TransactionId] = @TransactionId
        AND [ArchiveDetailId] = @ArchiveDetailId;
END
