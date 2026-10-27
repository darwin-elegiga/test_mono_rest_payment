CREATE PROCEDURE [dbo].[usp_Outbound835FileTransactionIds_By_Outbound835FileId]
    @Outbound835FileId BIGINT
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    SELECT      OT.Outbound835FileId, 
                OT.InboundPaymentId, 
                AP.TransactionId,
                F.PayerAlias, 
                F.OutboundRefEO, 
                F.FilePath, 
                F.[Status]             AS Outbound835FileStatus, 
                F.SentDateTime         AS Outbound835FileSentDateTime, 
                F.CreatedDateTime      AS Outbound835FileCreatedDateTime, 
                F.LastModifiedDateTime AS Outbound835FileLastModifiedDateTime
    FROM        dbo.Outbound835FileTransaction AS OT
                JOIN dbo.Outbound835File AS F 
                    ON F.Id = OT.Outbound835FileId
                LEFT JOIN dbo.ApMessageTracking AS AP 
                    ON AP.InboundPaymentId=OT.InboundPaymentId
    WHERE       Outbound835FileId = @Outbound835FileId;
END

