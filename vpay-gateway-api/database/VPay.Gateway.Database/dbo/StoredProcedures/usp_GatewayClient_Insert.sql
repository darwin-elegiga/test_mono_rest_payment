CREATE PROCEDURE [dbo].[usp_GatewayClient_Insert]
    @ClientId varchar(100),
    @ClientSecret varchar(256),
    @OriginCode varchar(50),
    @ClientCode varchar(10),
    @User varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.GatewayClient
        (
         ClientId,
         ClientSecret,
         OriginCode,
         ClientCode,
         CreatedBy,
         LastModifiedBy
        )
    VALUES
        (
         @ClientId,
         @ClientSecret,
         @OriginCode,
         @ClientCode,
         @User,
         @User
        )

    DECLARE @id BIGINT = SCOPE_IDENTITY();

    exec [dbo].[usp_GatewayClient_GetById] @id

END
GO
