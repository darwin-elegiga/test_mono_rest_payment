CREATE TABLE [dbo].OutboundPaymentClob
(
    Id                  BIGINT          NOT NULL    IDENTITY (1, 1) 
                                                    CONSTRAINT PK_OutboundPaymentClob PRIMARY KEY CLUSTERED (Id ASC)
,   InboundPaymentId    BIGINT          NOT NULL    CONSTRAINT FK_OutboundPaymentClob_InboundPaymentId FOREIGN KEY (InboundPaymentId) REFERENCES [dbo].InboundPayment (Id)
,   Clob                VARCHAR(MAX)    NOT NULL
,   CreatedDateTime     DATETIME2(7)    NOT NULL    CONSTRAINT DF_OutboundPaymentClob_CreatedDateTime DEFAULT(SYSDATETIME())
)
