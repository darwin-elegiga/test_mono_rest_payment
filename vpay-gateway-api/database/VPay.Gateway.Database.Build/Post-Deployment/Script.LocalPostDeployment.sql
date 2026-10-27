/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
:r ./db/Script.PostDeployment.sql

SET IDENTITY_INSERT dbo.GatewayClient ON

INSERT INTO dbo.GatewayClient
    (
     Id,
     ClientId,
     ClientSecret,
     OriginCode,
     ClientCode
    )
SELECT
    1,
    'local',
    '10000.sYqnrEek7+WP9hTULU7H8A==.70iKTPTPNybB1V0gCjyHTRr4I2MdK0j7kLSv3SUj6j+PIvmeav6EMMZd1NTDt+UaHuBzI4FUQPoJ0lxrME+vgw==',
    'VPA1',
    'VPA1'
WHERE NOT EXISTS (Select 1 from dbo.GatewayClient WHERE Id = 1)

SET IDENTITY_INSERT dbo.GatewayClient OFF

