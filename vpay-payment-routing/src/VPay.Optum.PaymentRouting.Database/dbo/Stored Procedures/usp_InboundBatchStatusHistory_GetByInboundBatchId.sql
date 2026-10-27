CREATE PROCEDURE [dbo].[usp_InboundBatchStatusHistory_GetByInboundBatchId]
    @InboundBatchId BIGINT
AS
BEGIN
SET NOCOUNT ON;


    SELECT      Id as InboundBatchStatusHistoryId,
                InboundBatchId,
                [Status], 
                CreatedDateTime
    FROM        [dbo].[InboundBatchStatusHistory]
    WHERE       InboundBatchId = @InboundBatchId;

END