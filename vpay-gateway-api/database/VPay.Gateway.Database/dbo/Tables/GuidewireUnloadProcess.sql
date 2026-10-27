CREATE TABLE [dbo].[GuidewireUnloadProcess]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ReasonCode] VARCHAR(4) NOT NULL, 
    [ProcessType] INT NOT NULL, 
    [Comment] VARCHAR(200) NOT NULL
)
GO


CREATE UNIQUE INDEX [ux_GuidewireUnloadProcess_ReasonCode] ON [dbo].[GuidewireUnloadProcess] ([ReasonCode])
GO