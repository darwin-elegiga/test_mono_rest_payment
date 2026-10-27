CREATE TABLE [dbo].[InboundHeader] (
    [Id]                   BIGINT        IDENTITY (1, 1) NOT NULL,
    [InboundBatchId]       BIGINT        NOT NULL,
    [ClobStorageId]        BIGINT        NOT NULL,
    [FileProcId]           VARCHAR (100) NULL,
    [PayerSubpayerId]      VARCHAR (10)  NULL,
    [PayerAlias]           VARCHAR (20)  NULL,
    [CreatedDateTime]      DATETIME2 (7) CONSTRAINT [df_InboundHeader_CreatedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    [CreatedBy]            VARCHAR (50)  NULL,
    [LastModifiedBy]       VARCHAR (50)  NULL,
    [LastModifiedDateTime] DATETIME2 (7) CONSTRAINT [DF_InboundHeader_LastModifiedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    [Status]               VARCHAR (20)  CONSTRAINT [DF_InboundHeader_Status] DEFAULT ('RECEIVED') NOT NULL,
    CONSTRAINT [PK_InboundHeader] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CHK_InboundHeader_Status] CHECK ([dbo].[udf_InboundPaymentStatus]([Status])=(1)),
    CONSTRAINT [FK_InboundHeader_ClobStorageId] FOREIGN KEY ([ClobStorageId]) REFERENCES [dbo].[ClobStorage] ([Id]),
    CONSTRAINT [FK_InboundHeader_InboundBatchId] FOREIGN KEY ([InboundBatchId]) REFERENCES [dbo].[InboundBatch] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [UNQ_InboundHeader_BatchId] UNIQUE NONCLUSTERED ([InboundBatchId] ASC)
);


