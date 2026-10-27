CREATE TABLE [dbo].InboundPaymentStatusHistory
(    
    [Id]                    BIGINT          NOT NULL IDENTITY (1, 1),
    [InboundPaymentId]      BIGINT          NOT NULL,
    [Status]                VARCHAR(20)     NOT NULL,
    CreatedDateTime         DATETIME2(7)    NOT NULL
        CONSTRAINT df_InboundPaymentStatusHistory_CreatedDateTime DEFAULT(SYSDATETIME()),
    CONSTRAINT [PK_InboundPaymentStatusHistory] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_InboundPaymentStatusHistory_InboundPaymentId] FOREIGN KEY (InboundPaymentId) REFERENCES [dbo].InboundPayment ([Id]) ON DELETE CASCADE,
)
