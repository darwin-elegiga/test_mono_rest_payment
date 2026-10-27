CREATE PROCEDURE [dbo].[usp_GatewayClient_GetById]
    @Id bigint
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
        Id = @Id
END
GO
