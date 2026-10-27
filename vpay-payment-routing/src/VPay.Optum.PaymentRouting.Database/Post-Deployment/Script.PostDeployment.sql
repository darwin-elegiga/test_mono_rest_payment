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


-- update new status column to latest from history
UPDATE InboundBatch
SET InboundBatch.[Status] = S.[Status]
FROM InboundBatch
INNER JOIN(
	SELECT C.InboundBatchId,C.CreatedDateTime, C.[Status]
	FROM(
	SELECT S.InboundBatchId, S.[Status], S.CreatedDateTime, Max(S.CreatedDateTime) OVER (PARTITION BY S.InboundBatchId) AS Most_Recent_Status
	FROM dbo.InboundBatchStatusHistory S
	) C
	WHERE C.CreatedDateTime = C.Most_Recent_Status AND C.[Status] != 'BatchInitiated'
) S ON S.InboundBatchId = Id
WHERE InboundBatch.[Status] = 'BatchInitiated';