CREATE TABLE [dbo].[Outbound835File] (
    [Id]                   BIGINT           IDENTITY (1, 1) NOT NULL,
    [PayerAlias]           VARCHAR (10)     NOT NULL,
    [OutboundRefEO]        VARCHAR (10)     NULL,
    [Status]               VARCHAR (20)     NOT NULL,
    [QueueMessageId]       UNIQUEIDENTIFIER NULL,
    [FilePath]             VARCHAR (256)    NULL,
    [CreatedBy]            VARCHAR (50)     NOT NULL,
    [CreatedDateTime]      DATETIME2 (7)    CONSTRAINT [DF_Outbound835File_CreatedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    [LastModifiedBy]       VARCHAR (50)     NOT NULL,
    [LastModifiedDateTime] DATETIME2 (7)    CONSTRAINT [DF_Outbound835File_LastModifiedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    [SentDateTime]         DATETIME2 (7)    NULL,
    [OutboundFinSegSequenceId] BIGINT NULL, 
    CONSTRAINT [PK_Outbound835File] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CK_Outbound835File_Status] CHECK ([dbo].[udf_Outbound835FileStatus]([Status])=(1)), 
    CONSTRAINT [FK_Outbound835File_OutboundFinSegSequence] FOREIGN KEY (OutboundFinSegSequenceId) REFERENCES dbo.OutboundFinSegSequence(Id)
);
GO
CREATE TRIGGER [dbo].[TRG_Outbound835File_Status_Update]
    ON [dbo].Outbound835File
    AFTER INSERT, UPDATE
AS
BEGIN
    SET NoCount ON;
    INSERT  dbo.Outbound835FileStatus (Outbound835FileId, [Status],[CreatedBy])
    SELECT  I.Id, I.[Status], COALESCE(I.LastModifiedBy, I.CreatedBy)
    FROM    Inserted I
END
GO

EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'Should be set when status is going to STARTED',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'Outbound835File',
    @level2type = N'COLUMN',
    @level2name = N'QueueMessageId'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'File path and name of the built file. Set when changes status to BUILT',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'Outbound835File',
    @level2type = N'COLUMN',
    @level2name = N'FilePath'
GO
EXEC sp_addextendedproperty @name = N'MS_Description',
    @value = N'will be set when XftrService notifies file sent and we change status to COMPLETED',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'Outbound835File',
    @level2type = N'COLUMN',
    @level2name = N'SentDateTime'