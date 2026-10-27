using System;
using System.Globalization;
using System.IO;

namespace VPay.DocSys.Parsing.Common;

public class StatusFilePathHelper : IStatusFilePathHelper
{
    public string CleanAndValidateStsPath(string stsFilePath)
    {
        var statusFileName = Path.GetFileName(stsFilePath);
        // Security: Validate path is indeed a file
        if (string.IsNullOrEmpty(statusFileName))
        {
            return null;
        }

        // validate filename doesn't have any illegal filesystem traversing characters
        if (statusFileName.Contains("..") || statusFileName.Contains("/") || statusFileName.Contains("\\"))
        {
            return null;
        }

        // Security: Validate file is in proper location
        if (!stsFilePath.StartsWith("/SRVFS/bogner/transfer/"))
        {
            return null;
        }

        var pathWithoutFile = Path.GetDirectoryName(stsFilePath);
        var pathTokens = pathWithoutFile.Split(Path.DirectorySeparatorChar);

        // example: /SRVFS/bogner/transfer/SEMED/S1M/2024/06/25/
        // pathTokens[0] is an empty string
        // pathTokens[1] would be "SRVFS"
        // pathTokens[8] would be "25"
        if (pathTokens.Length != 9)
        {
            return null;
        }

        var isValidDatePath = DateTime.TryParseExact(
                string.Concat(
                    pathTokens[6], // yyyy
                    pathTokens[7], // MM
                    pathTokens[8]),// dd
                new[] { "yyyyMMdd" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date);

        if (!(pathTokens[1].Equals("SRVFS")
            && pathTokens[2].Equals("bogner")
            && pathTokens[3].Equals("transfer")
            && isValidDatePath))
        {
            return null;
        }



        return Path.GetFullPath(
            Path.Combine(
                "/SRVFS/bogner/transfer/", //root dir
                pathTokens[4], // origin
                pathTokens[5], // tpa
                date.Year.ToString("0000"), // yyyy
                date.Month.ToString("00"), // MM
                date.Day.ToString("00"), // dd
                statusFileName));
    }
}

public interface IStatusFilePathHelper
{
    string CleanAndValidateStsPath(string stsFilePath);
}
