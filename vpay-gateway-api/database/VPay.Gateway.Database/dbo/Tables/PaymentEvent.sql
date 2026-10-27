CREATE TABLE [dbo].[PaymentEvent]
(
    [Id] BIGINT NOT NULL IDENTITY(1,1),
    [PaymentId] BIGINT NOT NULL,
    [TransactionId] BIGINT NOT NULL, 
    [TransactionEventId] BIGINT NOT NULL,
    [EventStatus] VARCHAR(10) NOT NULL,
    [EventDateTime] DATETIME2(7) NOT NULL, 
    [EventReasonCode] VARCHAR(4) NOT NULL, 
    [EventReasonDescription] VARCHAR(50) NOT NULL, 
    [CurrentStatus] VARCHAR(64) NOT NULL CONSTRAINT [df_PaymentEvent_CurrentStatus] DEFAULT 'SCHEDULED', 
    [CreatedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_PaymentEvent_CreatedDateTime] DEFAULT SYSDATETIME(), 
    [LastModifiedDateTime] DATETIME2 NOT NULL CONSTRAINT [df_PaymentEvent_LastModifiedDateTime] DEFAULT SYSDATETIME(), 
    [EventPaymentType] VARCHAR(10) NULL, 
    [EventTrackingNumber] VARCHAR(100) NULL, 
    CONSTRAINT [pk_PaymentEvent_Id] PRIMARY KEY CLUSTERED 
    (
        [Id] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT [FK_PaymentEvent_Payment] FOREIGN KEY (PaymentId) REFERENCES [Payment]([Id])
)

GO

CREATE INDEX [IX_PaymentEvent_PaymentId] ON [dbo].[PaymentEvent] ([PaymentId])

GO

CREATE INDEX [IX_PaymentEvent_TransactionId] ON [dbo].[PaymentEvent] ([TransactionId])
