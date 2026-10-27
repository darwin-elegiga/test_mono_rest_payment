CREATE TABLE [dbo].[ApMessageTrackingStatus] (
    [Id]                  BIGINT        IDENTITY (1, 1) NOT NULL,
    [ApMessageTrackingId] BIGINT        NOT NULL,
    [Status]              VARCHAR (20)  NOT NULL,
    [CreatedBy]           VARCHAR (50)  NOT NULL,
    [CreatedDateTime]     DATETIME2 (7) CONSTRAINT [DF_ApMessageTrackingStatus_CreatedDateTime] DEFAULT (sysdatetime()) NOT NULL,
    CONSTRAINT [PK_ApMessageTrackingStatus] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ApMessageTrackingStatus_ApMessageTrackingId] FOREIGN KEY ([ApMessageTrackingId]) REFERENCES [dbo].[ApMessageTracking] ([Id])
);




