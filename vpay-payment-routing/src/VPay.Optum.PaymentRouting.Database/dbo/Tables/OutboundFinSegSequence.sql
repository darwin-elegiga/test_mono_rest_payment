CREATE TABLE [dbo].[OutboundFinSegSequence] (
    [Id]                    BIGINT        IDENTITY (1, 1) NOT NULL,
    [PayerAlias]            VARCHAR (10)  NOT NULL,
    [DateStamp]             VARCHAR (8)   NOT NULL,
    [CurrentSequenceNumber] INT           CONSTRAINT [DF_OutboundFinSegSequence_CurrentSequenceNumber] DEFAULT ((51)) NOT NULL,
    [CreatedBy]             VARCHAR (50)  NULL,
    [CreatedDateTime]       DATETIME2 (7) CONSTRAINT [DF_OutboundFinSegSequence_CreatedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    [LastModifiedBy]        VARCHAR (50)  NULL,
    [LastModifiedDateTime]  DATETIME2 (7) NULL,
    CONSTRAINT [PK_OutboundFinSegSequence] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UNQ_OutboundFinSegSequence_PayerAlias_DateStamp] UNIQUE NONCLUSTERED ([PayerAlias] ASC, [DateStamp] ASC)
);



