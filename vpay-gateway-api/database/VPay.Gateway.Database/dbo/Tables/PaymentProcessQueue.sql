CREATE TABLE [dbo].[PaymentProcessQueue]
(
    [Id] BIGINT NOT NULL IDENTITY(1,1),
    [ClientCode] VARCHAR(10) NOT NULL,
    [ClientPaymentId] VARCHAR(100) NOT NULL,
    [CreatedDateTime] DATETIME2(7) NOT NULL CONSTRAINT [df_PaymentProcessQueue_CreatedDateTime] DEFAULT (SYSDATETIME()),
    [LastRequestedDateTime] DATETIME2(7) NOT NULL CONSTRAINT [df_PaymentProcessQueue_LastRequestedDateTime] DEFAULT (SYSDATETIME())
    CONSTRAINT [pk_PaymentProcessQueue_Id] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],    
    CONSTRAINT [uc_PaymentProcessQueue_ClientCode_ClientPaymentId] UNIQUE ([ClientCode], [ClientPaymentId])
)
