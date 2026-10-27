


Create   PROCEDURE [dbo].[usp_GetTransactionsByTrn02]  
    @StartDate DateTime,  
    @EndDate DateTime,  
    @Trn02 VARCHAR(20) ,  
    @ProviderTin VARCHAR(9) ,  
    @Page INT = NULL,  
    @PageSize INT = NULL  
AS  

--debug 
--declare  @StartDate DateTime  ='2025-01-01'
--declare   @EndDate DateTime='2025-02-20'
--declare    @Trn02 VARCHAR(20)='TEST'   
--declare    @ProviderTin VARCHAR(9)='999990001'  
--declare @Page INT  
--declare    @PageSize INT =1000


BEGIN  


    -- set default current page to first page  
    IF (@Page IS NULL OR @Page < 1)  
        SET @Page = 1;  
  
    -- set default page size for the number of records to return  
    IF (@PageSize IS NULL OR @PageSize <= 0)  
        SET @PageSize = 50;  
  
    SELECT 
	  BRDDTA.SETXID as vpayTransactionId,
	  BRDDTA.PaymentID  as paymentId,
	  PaymentCode as PaymentType,
	  BRDDTA.LoadDate as paymentDate,
	  BRDDTA.TAXID  as providerTin,
	  BRDDTA.TRN02 as trn02,
	  SCP.TMCLIC as ClientID,
	  SCP.TMBILC as BillingEntity

    FROM  [VPAY01].[VPAYBRDDTA_BRD835MAP]  BRDDTA   inner join 
	[VPAY01].[SEWCPS_CPSTMF] SCP on BRDDTA.SETXID=SCP.TMTXID
	where BRDDTA.TRN02=@Trn02 and BRDDTA.TAXID=@ProviderTin
	and (BRDDTA.LoadDate between @StartDate and @EndDate)
	ORDER BY vpayTransactionId  
     
 
    OFFSET      (@Page - 1) * @PageSize ROWS  
    FETCH NEXT  @PageSize ROWS ONLY  
    ;  
  
    -- select pagination data for the results found above  
    SELECT      @Page AS CurrentPage  
            ,   @PageSize AS PageSize  
            ,   COUNT(*) AS TotalRows  
    FROM  [VPAY01].[VPAYBRDDTA_BRD835MAP]  BRDDTA   inner join 
	[VPAY01].[SEWCPS_CPSTMF] SCP on BRDDTA.SETXID=SCP.TMTXID 
	where BRDDTA.TRN02=@Trn02 and BRDDTA.TAXID=@ProviderTin
	and (BRDDTA.LoadDate between @StartDate and @EndDate)
    
END  
GO



