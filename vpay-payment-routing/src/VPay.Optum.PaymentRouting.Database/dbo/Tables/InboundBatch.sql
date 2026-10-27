CREATE TABLE [dbo].InboundBatch
(
    [Id]                        BIGINT              IDENTITY (1, 1)         NOT NULL,
    [OptumBatchId]              VARCHAR(49)                                 NOT NULL,
    [Payer835Id]                VARCHAR(5)                                  NOT NULL,
    [TotalPaymentCount]         INT                                         NULL,
    [TotalPaymentAmount]        DECIMAL (20, 2)                             NULL,
    CreatedDateTime             DATETIME2(7)                                NOT NULL 
        CONSTRAINT df_InboundBatch_CreatedDateTime DEFAULT(SYSDATETIME()),
    LastModifiedDateTime        DATETIME2(7)                                NOT NULL 
        CONSTRAINT df_InboundBatch_LastModifiedDateTime DEFAULT(SYSDATETIME()),
    [Status] VARCHAR(20) NOT NULL DEFAULT ('BatchInitiated'), 
    CONSTRAINT [PK_InboundBatch] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UNQ_InboundBatch_OptumBatchId] UNIQUE NONCLUSTERED ([OptumBatchId] ASC)
)

GO

CREATE TRIGGER [dbo].[Trigger_InboundBatch_Status_Update]
    ON [dbo].[InboundBatch]
    AFTER INSERT, UPDATE
AS
BEGIN
    SET NoCount ON;
    INSERT  dbo.InboundBatchStatusHistory (InboundBatchId, [Status])
    SELECT  I.Id, I.[Status]
    FROM    Inserted I
END