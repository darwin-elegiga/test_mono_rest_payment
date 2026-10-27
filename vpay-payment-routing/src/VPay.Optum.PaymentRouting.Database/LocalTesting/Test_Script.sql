DECLARE @RC int

DECLARE @OptumBatchId varchar(49)                = 'TEST_005'
DECLARE @Payer835Id varchar(100)            = 'PAY1'
DECLARE @TotalPaymentCount int              = 1
DECLARE @TotalPaymentAmount decimal(12,4)   = 10.0

DECLARE @FileProcId varchar(100)            = 'File01'
DECLARE @PayerAlias varchar(100)            = 'PAY1_alias'
DECLARE @ProvTIN varchar(100)               = '100100100'
DECLARE @PayerSubpayerId varchar(100)       = 'sub-1'
DECLARE @ZipCode varchar(100)               = '12345'
DECLARE @BlobStorageURI varchar(max)  /*NULL*/
DECLARE @PaymentClob varchar(max)           ='Fake Data'

DECLARE @UserKey varchar(100) 
DECLARE @PaymentAmount decimal(12,4)
DECLARE @MessageType varchar(50)
 
 

DECLARE @TestData TABLE (
    Id INT IDENTITY(1,1),
    MessageType varchar(50),
    OptumBatchId varchar(49),
    Payer835Id varchar(100),
    UserKey varchar(100) NULL, 
    PaymentAmount decimal(12,4) NULL,
    TotalPaymentCount int NULL,
    TotalPaymentAmount decimal(12,4) NULL,
    PaymentClob varchar(max) NULL
)
INSERT @TestData (MessageType,OptumBatchId,Payer835Id, UserKey,PaymentAmount,TotalPaymentCount,TotalPaymentAmount, PaymentClob )
      SELECT 'BAT', 'TEST01', 'PAY1',  NULL,        NULL,     3, 99.0, NULL 
UNION SELECT '835', 'TEST01', 'PAY1', 'TEST01_001', 33.00, NULL, NULL, 'ST*835*0001~BPR*H*0*C*NON************20210318~TRN*1*618169391*1391995276*0000UMR01~REF*EV*841162789~DTM*405*20210302~N1*PR*UMR ROYAL NEIGHBORS OF AMERICA~N3*PO BOX 30541~N4*SALT LAKE CITY*UT*841300541~REF*2U*UMR01~PER*CX**TE*8005575087~PER*BL*UMR EDI TEAM*EM*UMR-EDI-IT-TEAM_DL@DS.UHC.COM~N1*PE*MEDICAL ARTS ASSOCIATES*XX*1396724183~N3*600 JOHN DEERE ROAD STE 200~N4*MOLINE*IL*61265~REF*TJ*362689597~LX*1~CLP*180003CFN*1*27*0*16.83*12*21055209420*11*1~NM1*QC*1*CARSTENS*JONATHON~NM1*IL*1*THOMPSON*KATHRYN****MI*16682822~NM1*82*1*BLASER MD*MARK****XX*1982683264~REF*1L*76030038~AMT*AU*27~SVC*HC:95117*27*0~DTM*472*20210218~CAS*CO*45*10.17~CAS*PR*1*16.83~REF*6R*180003CFNL1~AMT*B6*16.83~SE*29*0001~' 
UNION SELECT '835', 'TEST01', 'PAY1', 'TEST01_002', 33.00, NULL, NULL, 'ST*835*0001~BPR*H*0*C*NON************20210318~TRN*1*618169391*1391995276*0000UMR01~REF*EV*841162789~DTM*405*20210302~N1*PR*UMR ROYAL NEIGHBORS OF AMERICA~N3*PO BOX 30541~N4*SALT LAKE CITY*UT*841300541~REF*2U*UMR01~PER*CX**TE*8005575087~PER*BL*UMR EDI TEAM*EM*UMR-EDI-IT-TEAM_DL@DS.UHC.COM~N1*PE*MEDICAL ARTS ASSOCIATES*XX*1396724183~N3*600 JOHN DEERE ROAD STE 200~N4*MOLINE*IL*61265~REF*TJ*362689597~LX*1~CLP*180003CFN*1*27*0*16.83*12*21055209420*11*1~NM1*QC*1*CARSTENS*JONATHON~NM1*IL*1*THOMPSON*KATHRYN****MI*16682822~NM1*82*1*BLASER MD*MARK****XX*1982683264~REF*1L*76030038~AMT*AU*27~SVC*HC:95117*27*0~DTM*472*20210218~CAS*CO*45*10.17~CAS*PR*1*16.83~REF*6R*180003CFNL1~AMT*B6*16.83~SE*29*0001~' 
UNION SELECT '835', 'TEST01', 'PAY1', 'TEST01_003', 33.00, NULL, NULL, 'ST*835*0001~BPR*H*0*C*NON************20210318~TRN*1*618169391*1391995276*0000UMR01~REF*EV*841162789~DTM*405*20210302~N1*PR*UMR ROYAL NEIGHBORS OF AMERICA~N3*PO BOX 30541~N4*SALT LAKE CITY*UT*841300541~REF*2U*UMR01~PER*CX**TE*8005575087~PER*BL*UMR EDI TEAM*EM*UMR-EDI-IT-TEAM_DL@DS.UHC.COM~N1*PE*MEDICAL ARTS ASSOCIATES*XX*1396724183~N3*600 JOHN DEERE ROAD STE 200~N4*MOLINE*IL*61265~REF*TJ*362689597~LX*1~CLP*180003CFN*1*27*0*16.83*12*21055209420*11*1~NM1*QC*1*CARSTENS*JONATHON~NM1*IL*1*THOMPSON*KATHRYN****MI*16682822~NM1*82*1*BLASER MD*MARK****XX*1982683264~REF*1L*76030038~AMT*AU*27~SVC*HC:95117*27*0~DTM*472*20210218~CAS*CO*45*10.17~CAS*PR*1*16.83~REF*6R*180003CFNL1~AMT*B6*16.83~SE*29*0001~' 
UNION SELECT 'EOF', 'TEST01', 'PAY1',  NULL,        NULL,  NULL, NULL, 'ISA*00*          *00*          *ZZ*391995276      *ZZ*841162789      *210315*0712*^*00501*554869391*0*P*:~GS*HP*391995276*841162789*20210315*0712*120834502*X*005010X221A1~'

-- Order that things get into the db shouldn't matter.
UNION SELECT 'EOF', 'TEST02', 'PAY1',  NULL,        NULL,  NULL, NULL, 'ISA*00*          *00*          *ZZ*391995276      *ZZ*841162789      *210315*0712*^*00501*554869391*0*P*:~GS*HP*391995276*841162789*20210315*0712*120834502*X*005010X221A1~'
UNION SELECT '835', 'TEST02', 'PAY1', 'TEST02_001', 22.00, NULL, NULL, 'ST*835*0001~BPR*H*0*C*NON************20210318~TRN*1*618169391*1391995276*0000UMR01~REF*EV*841162789~DTM*405*20210302~N1*PR*UMR ROYAL NEIGHBORS OF AMERICA~N3*PO BOX 30541~N4*SALT LAKE CITY*UT*841300541~REF*2U*UMR01~PER*CX**TE*8005575087~PER*BL*UMR EDI TEAM*EM*UMR-EDI-IT-TEAM_DL@DS.UHC.COM~N1*PE*MEDICAL ARTS ASSOCIATES*XX*1396724183~N3*600 JOHN DEERE ROAD STE 200~N4*MOLINE*IL*61265~REF*TJ*362689597~LX*1~CLP*180003CFN*1*27*0*16.83*12*21055209420*11*1~NM1*QC*1*CARSTENS*JONATHON~NM1*IL*1*THOMPSON*KATHRYN****MI*16682822~NM1*82*1*BLASER MD*MARK****XX*1982683264~REF*1L*76030038~AMT*AU*27~SVC*HC:95117*27*0~DTM*472*20210218~CAS*CO*45*10.17~CAS*PR*1*16.83~REF*6R*180003CFNL1~AMT*B6*16.83~SE*29*0001~' 
UNION SELECT 'BAT', 'TEST02', 'PAY1',  NULL,        NULL,     1, 22.0, NULL 


SELECT * FROM @TestData

/*
The "end result" of the inserts should be the overall status of the batch is returned as a single row in a results set.
To be "good", there must be no null values in any columns, there must be exactly 1 eof and the counts and amounts should be equal.



*/




declare @i INT 

SELECT TOP 1 @i=id FROM @TestData ORDER BY Id ASC
while ( @i <= (Select count(1) FROM @TestData) )
BEGIN
    SELECT @MessageType=MessageType,
        @OptumBatchId=OptumBatchId,
        @Payer835Id= Payer835Id, 
        @UserKey = UserKey,
        @PaymentAmount= PaymentAmount,
        @TotalPaymentCount = TotalPaymentCount,
        @TotalPaymentAmount = TotalPaymentAmount,
        @PaymentClob = PaymentClob
    FROM @TestData 
    WHERE Id = @i

    IF (@MessageType = 'BAT')
    BEGIN
    --PRINT @MessageType
        EXEC @RC = [dbo].[usp_InboundBatch_Insert] @OptumBatchId,@Payer835Id,@TotalPaymentCount,@TotalPaymentAmount
        EXEC [dbo].[usp_InboundBatch_Summary] @RC
    END

    IF (@MessageType = '835')
    BEGIN
        --PRINT @MessageType
        EXEC @RC = dbo.[usp_InboundPayment_Insert] @OptumBatchId,@Payer835Id,@FileProcId,@PayerAlias ,@ProvTIN,@PayerSubpayerId,@ZipCode,@UserKey,@PaymentAmount,@PaymentClob
    END

    IF (@MessageType = 'EOF')
    BEGIN
        --PRINT @MessageType
          EXEC @RC = [dbo].[usp_InboundHeader_Insert] @OptumBatchId,@Payer835Id,@PayerSubpayerId,@FileProcId,@PayerAlias,@PaymentClob
    END
    SELECT @i = @i+1
END

-- DELETE FROM dbo.InboundBatch where OptumBatchId IN ('TEST01', 'TEST02')


/*

-- This is what a "whole" file should look like...
SELECT X.OptumBatchId,  X.Payer835Id,  X.RecordType,  X.PaymentClob FROM
(
SELECT B.OptumBatchId, B.Payer835Id, 1 as DisplayOrder, 'EOF' as RecordType, EP.PaymentClob
  FROM dbo.InboundBatch as B
          JOIN dbo.InboundHeader as E
            ON E.InboundBatchId=B.Id
        JOIN dbo.ClobStorage as EP
            ON  E.ClobStorageId=EP.Id    
UNION ALL        
SELECT B.OptumBatchId, B.Payer835Id, 2 as DisplayOrder, '835' as RecordType, CP.PaymentClob
  FROM dbo.InboundBatch as B
        JOIN dbo.InboundPayment as P
            ON P.InboundBatchId=B.Id
        JOIN dbo.ClobStorage as CP
            ON  P.ClobStorageId=CP.Id
) X
ORDER BY X.OptumBatchId, X.DisplayOrder




*/