CREATE TABLE [dbo].[Outbound835FileStatus] (
    [Id]                BIGINT        IDENTITY (1, 1) NOT NULL,
    [Outbound835FileId] BIGINT        NOT NULL,
    [Status]            VARCHAR (20)  NOT NULL,
    [CreatedBy]         VARCHAR (50)  NOT NULL,
    [CreatedDateTime]   DATETIME2 (7) CONSTRAINT [DF_Outbound835FileStatus_CreatedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    CONSTRAINT [PK_Outbound835FileStatus] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Outbound835FileStatus_Outbound835FileId] FOREIGN KEY ([Outbound835FileId]) REFERENCES [dbo].[Outbound835File] ([Id])
);




GO
