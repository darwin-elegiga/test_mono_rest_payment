CREATE PROCEDURE [dbo].[usp_OutboundFinSegSequence_GetById]
    @OutboundFinSegSequenceId BIGINT
AS
BEGIN
    SET XACT_ABORT ON;
    SET NOCOUNT ON;
    SELECT      Id AS OutboundFinSegSequenceId,
                PayerAlias,
                DateStamp,
                CurrentSequenceNumber,
                CreatedBy,
                CreatedDateTime,
                [LastModifiedBy],
                [LastModifiedDateTime]
    FROM        dbo.OutboundFinSegSequence 
    WHERE       Id = @OutboundFinSegSequenceId;
END
