using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using VPay.DocSys.Common;
using VPay.DocSys.Parsing.Common.Interfaces;

namespace VPay.DocSys.Parsing
{
    public class OTBParserService : IOTBParserService
    {
        private readonly ILogger _logger;

        public OTBParserService(ILogger<OTBParserService> logger)
        {
            _logger = logger;
        }

        public IDictionary<string, IDictionary<string, string>> ParseOTBFile(string filePath, string billingEntityKey = "BillingEntity")
        {
            using (var reader = new StreamReader(filePath))
            {
                var result = ParseOTBData(reader, filePath, billingEntityKey);
                reader.Close();

                return result;
            }
        }

        public IDictionary<string, IDictionary<string, string>> ParseOTBData(StreamReader otbFileStream, string billingEntityKey = "BillingEntity")
        {
            return ParseOTBData(otbFileStream, null, billingEntityKey);
        }

        private IDictionary<string, IDictionary<string, string>> ParseOTBData(StreamReader otbFileStream, string fileName, string billingEntityKey)
        {
            var beKey = billingEntityKey;
            var result = new Dictionary<string, IDictionary<string, string>>();

            _logger.LogInformation("Start Processing OTB File [{FileName}]", fileName);

            string line = otbFileStream.ReadLine();

            if (String.IsNullOrWhiteSpace(line))
            {
                _logger.LogWarning("First line was empty on file [{FileName}]", fileName);
                return null;
            }

            if (!line.StartsWith("00|"))
            {
                ThrowInvalidFileError(fileName, "First Record in otb file is not \"00|\"");
            }

            var headerLine = line.Split('|');
            var beIndex = Array.IndexOf(headerLine, beKey);

            if (beIndex < 0)
            {
                ThrowInvalidFileError(fileName, $"Header record missing \"{beKey}\" in otb file");
            }

            int count = 1;

            while (!String.IsNullOrWhiteSpace(line = otbFileStream.ReadLine()))
            {
                count++;
                if (line.StartsWith("99|"))
                {
                    //If line ends with 99| then end loop
                    break;
                }
                else if (!line.StartsWith("42|"))
                {
                    //If line does not start with 42 then the file is invalid.
                    ThrowInvalidFileError(fileName, $"Record {count} has no \"42|\" intro");
                }

                var oneList = line.Split('|');

                if (oneList.Length != headerLine.Length)
                {
                    ThrowInvalidFileError(fileName, $"Record {count} has bad field count ({oneList.Length} sb {headerLine.Length}");
                }

                if (result.ContainsKey(oneList[beIndex]))
                {
                    ThrowInvalidFileError(fileName, $"*** DUPLICATE ({beKey}: {oneList[beIndex]}");
                }

                var dict = new Dictionary<string, string>();

                for (var i = 1; i < headerLine.Length - 1; i++)
                {
                    dict.Add(headerLine[i], oneList[i]);
                }

                result.Add(oneList[beIndex], dict);

            }

            _logger.LogInformation("Completed Processing OTB File [{FileName}]", fileName);

            return result;
        }


        private void ThrowInvalidFileError(string fileName, string message)
        {
            _logger.LogError($"{message}{Environment.NewLine}FileName = {{FileName}}", fileName);
            throw new InvalidFileFormatException(fileName, message);
        }
    }
}
