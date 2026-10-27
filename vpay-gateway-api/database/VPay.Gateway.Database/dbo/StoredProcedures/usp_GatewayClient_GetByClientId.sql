CREATE PROCEDURE [dbo].[usp_GatewayClient_GetByClientId]
    @ClientId varchar(100)
AS
BEGIN    
    SET NOCOUNT ON;

    SELECT
        Id
        , ClientId
        , ClientSecret
        , OriginCode
        , ClientCode
        , CreatedBy
        , LastModifiedBy
        , CreatedDateTime
        , LastModifiedDateTime
        , ExpiredDateTime
    FROM
        dbo.GatewayClient
    WHERE
        ClientId = @ClientId
END
GO
