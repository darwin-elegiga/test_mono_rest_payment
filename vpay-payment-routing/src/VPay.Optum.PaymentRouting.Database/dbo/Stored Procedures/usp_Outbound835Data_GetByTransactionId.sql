CREATE PROCEDURE [dbo].[usp_Outbound835Data_GetByTransactionId]
  @TransactionId BIGINT
AS
BEGIN
SET NOCOUNT ON;

	SELECT
		a.[TransactionId],
		p.[OutboundRefEO],
		cs.[PaymentClob] AS HeaderData,
		pc.[Clob] AS PaymentData
	FROM [Archive].[ApMessageTracking] a
		INNER JOIN [Archive].[InboundPayment] p ON
		  p.Id = a.InboundPaymentId
		INNER JOIN [Archive].[InboundHeader] e ON
		  e.InboundBatchId = p.InboundBatchId
		INNER JOIN [Archive].[ClobStorage] cs ON
	      cs.Id = e.ClobStorageId
		INNER JOIN [Archive].[OutboundPaymentClob] pc ON
		  pc.InboundPaymentId = p.Id
	WHERE
	  a.TransactionId = @TransactionId
	;

END
