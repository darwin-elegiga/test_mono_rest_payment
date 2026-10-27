using System;
using System.Collections.Generic;
using System.Linq;
using VPay.DocSys.Common;

namespace VPay.DocSys.Parsing.Common
{
    public class StatusFileInfo
    {
        public StatusFileInfo(IStatusFilePathHelper statusFilePathHelper, string statusFilePath)
        {
            if (string.IsNullOrEmpty(statusFilePath))
            {
                throw new ArgumentNullException(nameof(statusFilePath));
            }

            if (statusFilePathHelper is null)
            {
                throw new ArgumentNullException(nameof(statusFilePathHelper));
            }

            var validStsPath = statusFilePathHelper.CleanAndValidateStsPath(statusFilePath);
            if (validStsPath is null)
            {
                throw new ArgumentException("Invalid status file path");
            }

            if (!System.IO.File.Exists(validStsPath))
            {
                throw new System.IO.FileNotFoundException("StatusFileInfo cannot find StatusFilePath", validStsPath);
            }
            StatusFileDirectory = System.IO.Path.GetDirectoryName(validStsPath) + System.IO.Path.DirectorySeparatorChar;
            StatusFileName = System.IO.Path.GetFileName(validStsPath);
            StatusFile = new StatusFile(System.IO.File.ReadLines(validStsPath).ToArray());
            StatusFilePath = validStsPath;
        }
        public string StatusFileName { get; }
        public string StatusFilePath { get; }
        public string StatusFileDirectory { get; }
        public List<long> TransactionIds { get; } = new List<long>();
        public StatusFile StatusFile { get; }
    }
}
