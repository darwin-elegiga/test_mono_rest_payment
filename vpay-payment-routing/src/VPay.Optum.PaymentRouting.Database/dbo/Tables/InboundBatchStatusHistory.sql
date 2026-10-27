CREATE TABLE [dbo].InboundBatchStatusHistory
(    
    [Id]                    BIGINT          NOT NULL IDENTITY (1, 1),
    [InboundBatchId]        BIGINT          NOT NULL,
    [Status]                VARCHAR(20)    NOT NULL,
    CreatedDateTime         DATETIME2(7)    NOT NULL
        CONSTRAINT df_InboundBatchStatusHistory_CreatedDateTime DEFAULT(SYSDATETIME()),
    CONSTRAINT [PK_InboundBatchStatusHistory] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_InboundBatchStatusHistory_InboundBatchId] FOREIGN KEY (InboundBatchId) REFERENCES [dbo].InboundBatch ([Id]) ON DELETE CASCADE,
)
