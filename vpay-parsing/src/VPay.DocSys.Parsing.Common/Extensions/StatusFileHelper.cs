using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common.Extensions
{
    public static class StatusFileHelper
    {
        public static bool IsEmergencyFile(this string statusFilePath)
        {
            return Path.GetFileName(statusFilePath).StartsWith("E", StringComparison.InvariantCultureIgnoreCase);
        }
        public static bool IsDropToPrintFile(this string statusFilePath)
        {
            return Path.GetFileName(statusFilePath).StartsWith("PF", StringComparison.InvariantCultureIgnoreCase);
        }
        public static bool IsReIssueFile(this string statusFilePath)
        {
            return Path.GetFileName(statusFilePath).StartsWith("R", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsBoardingFile(this string statusFilePath)
        {
            return Path.GetFileName(statusFilePath).StartsWith("B", StringComparison.InvariantCultureIgnoreCase);
        }

        public static string GetOrigin(this StatusFile statusFile)
        {
            List<StatusRecord> records = statusFile.Records;
            return (records?.FirstOrDefault()?.ORIGIN ?? string.Empty);
        }

        public static bool IsPaymentEngineConfirmationProcessing(this string origin, string statusFileName, IConfiguration configuration)
        {
            if (!statusFileName.IsBoardingFile())
            {
                return false;
            }

            var peConfirmationOriginCodes = configuration.GetSection("StatusFile").GetValue<string>("PEConfirmationOriginCodes") ?? "PE";
            if (string.IsNullOrWhiteSpace(peConfirmationOriginCodes))
            {
                return false;
            }

            var peConfirmationTPAList = peConfirmationOriginCodes.Split(",").Select(x => x.ToLower()).ToList();
            return peConfirmationTPAList.Contains(origin.ToLower());
        }
    }
}
