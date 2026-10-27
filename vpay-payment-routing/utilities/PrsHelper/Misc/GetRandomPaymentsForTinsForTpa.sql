-- on STGL-Platform, this will get the json to create the sample-<TPA>-batch.json info...

SELECT 
REPLACE(Replace('{"ProvTin": "272204037","PayerSubpayerId": "$OptumSubPayorId","ZipCode": "54402","Amount": $amt},'
,'$OptumSubPayorId',[OptumSubPayorId])
,'$amt',  Cast( 
 ((Rand(ABS(CAST(CAST(NEWID() AS VARBINARY) AS INT))) * 100000)+1)/100.0 as decimal(10,2))

),*
  FROM [PlatformConfig].[dbo].[PrsMapping]
  where ClientCode ='MDW'