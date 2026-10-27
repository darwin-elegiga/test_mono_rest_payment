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

MERGE dbo.GuidewireUnloadProcess AS target
USING (
    SELECT *
        FROM ( VALUES
               ('4000', 0, '')
              ,('4001', 2, 'User Unload and Reissue')
              ,('4004', 3, 'Automated Unload - Card Expired')
              ,('4005', 2, 'Automated Unload and Issue Check to Payee due to No Activity')
              ,('4008', 3, 'Automated Unload and Cancel Payment')
              ,('4009', 3, 'Automated Unload -Check Expired')
              ,('4010', 2, 'Automated Unload and Reissue Check to Payee')
              ,('4011', 2, 'Automated Unload and Reissue Check to Payee Due to No Activity')
              ,('4014', 2, 'User Unload and Reissue')
              ,('4026', 3, 'Payer Unload Request')
              ,('4027', 3, 'Client Unload and Return to client')
              ,('4028', 3, 'Payer Unload')
              ,('4029', 3, 'Payer Unload and Cancel Payment')
              ,('4030', 3, 'Payer Unload')
              ,('4031', 3, 'Payee Unload')
              ,('4032', 3, 'Payee Unload and Cancel Payment')
              ,('4033', 3, 'Payee Unload')
              ,('4034', 3, 'Payee Unload and Cancel Payment')
              ,('4035', 3, 'Payee Unload')
              ,('4036', 3, 'Returned Mail Unload')
              ,('4037', 3, 'Returned Mail Unload and Cancel Payment')
              ,('4038', 3, 'Returned Mail Unload')
              ,('4039', 3, 'Returned Mail Unload and Cancel Payment')
              ,('4040', 3, 'Returned Mail Unload')
              ,('4041', 3, 'Bad Data Unload and Cancel Payment')
              ,('4042', 2, 'Automated Unload and Issue Check to Payee')
              ,('4043', 3, 'Bad Data Unload')
              ,('4044', 3, 'Client Request Auto Unload')
              ,('4045', 1, 'Automated Unload and ESCHEAT DUE TO Expiration')
              ,('4046', 3, 'Cancel due to CASS Address Failure')
              ,('4047', 3, 'Cancel due to CASS Address Failure')
              ,('4048', 3, 'Cancel due to CASS Address Failure')
              ,('4049', 3, 'Cancel due to CASS Address Failure')
              ,('4050', 3, 'Cancel due to CASS Address Failure')
              ,('4052', 3, 'Payer Unload and Client determine next steps')
              ,('4053', 3, 'Payer Unload and Client determine next steps')
              ,('4058', 2, 'Payer One Time Unload and VPay Reissue Payment')
              ,('4062', 3, 'Automated Unload and Cancel Payment')
              ,('4063', 3, 'Automated Unload')
              ,('4901', 3, 'Payer Err Unload and Cncl Payment')
              ,('4902', 3, 'Payer Err Unload')
              ,('4903', 3, 'Payer Err Unload and Cncl Payment')
              ,('4904', 3, 'Payer Err Unload')
              ,('4905', 3, 'Sys Err Unload and Cancel Payment')
              ,('4906', 3, 'Sys Err Unload and Cancel Payment')
              ,('4907', 0, '')
              ,('4909', 3, 'Penny Test-Unload and Cancel Pmt')
              ,('4910', 3, 'Penny Test-Unload and Cancel Pmt')
              ,('4912', 3, 'Penny Test-Unload Request')
              ,('4913', 3, 'Sys Err Bad Data Unload')
              ,('4914', 3, 'Penny Test-Unload and Cancel Pmt')
             ) as s (ReasonCode, ProcessType, Comment)
    )
    AS source
ON (target.ReasonCode = source.ReasonCode)
WHEN NOT MATCHED THEN
    INSERT (ReasonCode, ProcessType, Comment)
    VALUES (source.ReasonCode, source.ProcessType, source.Comment)
;