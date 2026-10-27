CREATE TABLE [dbo].[ClobStorage]
(
    [Id]                    BIGINT          NOT NULL    IDENTITY (1, 1),
    [PaymentClob]           VARCHAR (MAX)   NULL,
    CreatedDateTime         DATETIME2(7)    NOT NULL    
        CONSTRAINT df_ClobStorage_CreatedDateTime DEFAULT(SYSDATETIME()),
    CONSTRAINT [PK_ClobStorage] PRIMARY KEY CLUSTERED ([Id] ASC),
)
