CREATE TABLE [dbo].[GatewayClient]
(
    [Id] BIGINT NOT NULL IDENTITY(1,1), 
    [ClientId] VARCHAR(100) NOT NULL, 
    [ClientSecret] VARCHAR(256) NOT NULL, 
    [OriginCode] VARCHAR(50) NOT NULL, 
    [ClientCode] VARCHAR(10) NOT NULL, 
    [CreatedBy] VARCHAR(50) NOT NULL CONSTRAINT [df_GatewayClient_CreatedBy] DEFAULT (''), 
    [LastModifiedBy] VARCHAR(50) NOT NULL CONSTRAINT [df_GatewayClient_LastModifiedBy]  DEFAULT (''), 
    [CreatedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_GatewayClient_CreatedDateTime] DEFAULT (SYSDATETIME()), 
    [LastModifiedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_GatewayClient_LastModifiedDateTime] DEFAULT (SYSDATETIME()), 
    [ExpiredDateTime] DATETIME2 NULL
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[GatewayClient] ADD CONSTRAINT [pk_GatewayClient_Id] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO

CREATE UNIQUE INDEX [ux_GatewayClient_ClientId] ON [dbo].[GatewayClient] ([ClientId])
