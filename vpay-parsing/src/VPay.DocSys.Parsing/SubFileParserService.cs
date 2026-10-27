using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing
{
    public class SubFileParserService : ISubFileParserService
    {

        private readonly ILogger _logger;

        public SubFileParserService(ILogger<SubFileParserService> logger)
        {
            _logger = logger;
        }


        public IDictionary<string, SubstituteDataInfo> ParseFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                var result = ParseData(reader, filePath);
                reader.Close();

                return result;
            }
        }

        public IDictionary<string, SubstituteDataInfo> ParseData(StreamReader fileStream)
        {
            return ParseData(fileStream, null);
        }


        private IDictionary<string, SubstituteDataInfo> ParseData(StreamReader fileStream, string fileName)
        {
            var result = new Dictionary<string, SubstituteDataInfo>();

            _logger.LogInformation("Start Processing SUB File [{FileName}]", fileName);

            string line = fileStream.ReadLine();

            if (String.IsNullOrWhiteSpace(line))
            {
                _logger.LogWarning("First line was empty on file [{FileName}]", fileName);
                return null;
            }

            if (!line.StartsWith("00|"))
            {
                ThrowInvalidFileError(fileName, "First Record in sub file is not \"00|\"");
            }

            /* These are the fields in each detail record:
            *{
            *    "rec_id", "txid", "address_1", "address_2", "city",
            *    "state", "zip", "pay_type", "orig_dist", "payee_name",
            *    "payee_phone", "payee_fax", "origin_name", "tpa",
            *    "billing_entity", "vpgrp01", "vpgrp02", "vpgrp03",
            *    "vpgrp04", "vpgrp05", "vpgrp06"
            *}
            */

            //detailLineLength = count of expected fields + 1 (because the file always has an extra pipe at end of line)
            var detailLineLength = 22;

            int count = 1;

            while (!String.IsNullOrWhiteSpace(line = fileStream.ReadLine()))
            {
                count++;
                if (line.StartsWith("99|"))
                {
                    //If line ends with 99| then end loop
                    break;
                }
                else if (!line.StartsWith("23|"))
                {
                    //If line does not start with 23 then the file is invalid.
                    ThrowInvalidFileError(fileName, $"Record {count} has no \"23|\" intro");
                }

                var oneList = line.Split('|');

                if (oneList.Length != detailLineLength)
                {
                    ThrowInvalidFileError(fileName, $"Record {count} has bad field count ({oneList.Length} sb {detailLineLength})");
                }

                var data = new SubstituteDataInfo
                {
                    RecordId = "23",
                    TXID = oneList[1],
                    Address1 = oneList[2],
                    Address2 = oneList[3],
                    City = oneList[4],
                    State = oneList[5],
                    Zip = oneList[6],
                    PayType = oneList[7],
                    OrigDist = oneList[8],
                    PayeeName = oneList[9],
                    PayeePhone = oneList[10],
                    PayeeFax = oneList[11],
                    OriginName = oneList[12],
                    TPA = oneList[13],
                    BillingEntity = oneList[14],
                    VpGrp01 = oneList[15],
                    VpGrp02 = oneList[16],
                    VpGrp03 = oneList[17],
                    VpGrp04 = oneList[18],
                    VpGrp05 = oneList[19],
                    VpGrp06 = oneList[20]
                };

                data.CityStateZip = $"{data.City}, {data.State} {data.Zip}";

                if (result.ContainsKey(data.TXID))
                {
                    ThrowInvalidFileError(fileName, $"*** DUPLICATE (txid: {data.TXID})");
                }

                result.Add(data.TXID, data);

            }

            _logger.LogInformation("Completed Processsing SUB File [{FileName}]", fileName);

            return result;
        }

        private void ThrowInvalidFileError(string fileName, string message)
        {
            _logger.LogError($"{message}{Environment.NewLine}FileName = {{FileName}}", fileName);
            throw new InvalidFileFormatException(fileName, message);
        }
    }
}
